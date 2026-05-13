-- Virtual Queue Management System
-- PostgreSQL init script
-- Built from the logical data model.

-- =========================
-- ENUM TYPES (удалены, заменены на VARCHAR)
-- =========================

-- =========================
-- TABLES
-- =========================
CREATE TABLE IF NOT EXISTS users (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    login VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(255) NOT NULL,
    last_name VARCHAR(255) NOT NULL,
    email VARCHAR(255) UNIQUE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS user_sessions (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE ON UPDATE CASCADE,
    token VARCHAR(255) NOT NULL UNIQUE,
    ip_address VARCHAR(45),
    user_agent TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMP NOT NULL,
    last_activity_at TIMESTAMP NOT NULL DEFAULT NOW(),
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS roles (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    code VARCHAR(50) NOT NULL UNIQUE,
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS user_roles (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE ON UPDATE CASCADE,
    role_id INTEGER NOT NULL REFERENCES roles(id) ON DELETE CASCADE ON UPDATE CASCADE,
    assigned_at TIMESTAMP NOT NULL DEFAULT NOW(),
    assigned_by INTEGER REFERENCES users(id) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT uq_user_roles_user_role UNIQUE (user_id, role_id)
);

CREATE TABLE IF NOT EXISTS client_sessions (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    device_fingerprint VARCHAR(255) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMP NOT NULL DEFAULT (NOW() + INTERVAL '24 hours'),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    ip_address VARCHAR(45),
    user_agent TEXT
);

CREATE TABLE IF NOT EXISTS queue_configs (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    distribution_mode VARCHAR(20) NOT NULL DEFAULT 'MANUAL',
    is_service_type_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    is_priority_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    priority_escalation_wait_min INTEGER DEFAULT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by_id INTEGER NOT NULL REFERENCES users(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT chk_queue_configs_priority_wait CHECK (
        priority_escalation_wait_min IS NULL OR priority_escalation_wait_min >= 0
    )
);

CREATE TABLE IF NOT EXISTS service_types (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    queue_config_id INTEGER NOT NULL REFERENCES queue_configs(id) ON DELETE CASCADE ON UPDATE CASCADE,
    name VARCHAR(255) NOT NULL,
    code VARCHAR(50) NOT NULL UNIQUE,
    letter CHAR(1) NOT NULL,
    base_priority_level INTEGER NOT NULL DEFAULT 0,
    plan_avg_service_time_sec INTEGER,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_highlighting BOOLEAN NOT NULL DEFAULT FALSE,
    sort_order INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_service_types_priority CHECK (base_priority_level >= 0),
    CONSTRAINT chk_service_types_plan_time CHECK (
        plan_avg_service_time_sec IS NULL OR plan_avg_service_time_sec > 0
    ),
    CONSTRAINT uq_service_types_queue_letter UNIQUE (queue_config_id, letter)
);

CREATE TABLE IF NOT EXISTS queue_sessions (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    queue_config_id INTEGER NOT NULL REFERENCES queue_configs(id) ON DELETE CASCADE ON UPDATE CASCADE,
    status VARCHAR(20) NOT NULL DEFAULT 'DRAFT',
    started_at TIMESTAMP NULL,
    closed_at TIMESTAMP NULL,
    created_by INTEGER NOT NULL REFERENCES users(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_queue_sessions_dates CHECK (
        closed_at IS NULL OR started_at IS NULL OR closed_at >= started_at
    )
);

CREATE TABLE IF NOT EXISTS tickets (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    queue_session_id INTEGER NOT NULL REFERENCES queue_sessions(id) ON DELETE CASCADE ON UPDATE CASCADE,
    service_type_id INTEGER NULL REFERENCES service_types(id) ON DELETE SET NULL ON UPDATE CASCADE,
    ticket_number VARCHAR(20) NOT NULL,
    client_name VARCHAR(100) NOT NULL,
    client_surname VARCHAR(100) NOT NULL,
    sort_order NUMERIC(20,10) NOT NULL,
    priority_level INTEGER NOT NULL DEFAULT 0,
    status VARCHAR(20) NOT NULL DEFAULT 'WAITING',
    version INTEGER NOT NULL DEFAULT 1,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    called_at TIMESTAMP NULL,
    service_started_at TIMESTAMP NULL,
    service_ended_at TIMESTAMP NULL,
    updated_at TIMESTAMP DEFAULT NOW(),
    served_by_user_id INTEGER NULL REFERENCES users(id) ON DELETE SET NULL ON UPDATE CASCADE,
    client_session_id INTEGER NULL REFERENCES client_sessions(id) ON DELETE SET NULL ON UPDATE CASCADE,
    cancel_reason TEXT NULL,
    CONSTRAINT uq_tickets_queue_ticket_number UNIQUE (queue_session_id, ticket_number),
    CONSTRAINT chk_tickets_sort_order CHECK (sort_order >= 0),
    CONSTRAINT chk_tickets_priority_level CHECK (priority_level >= 0),
    CONSTRAINT chk_tickets_version CHECK (version >= 1),
    CONSTRAINT chk_tickets_called_at CHECK (
        called_at IS NULL OR called_at >= created_at
    ),
    CONSTRAINT chk_tickets_service_started_at CHECK (
        service_started_at IS NULL OR service_started_at >= created_at
    ),
    CONSTRAINT chk_tickets_service_ended_at CHECK (
        service_ended_at IS NULL OR service_started_at IS NULL OR service_ended_at >= service_started_at
    ),
    CONSTRAINT chk_tickets_served_requires_end_time CHECK (
        (status = 'SERVED' AND service_ended_at IS NOT NULL) OR status <> 'SERVED'
    ),
    CONSTRAINT chk_tickets_skipped_cancelled_require_end_time CHECK (
        (status IN ('SKIPPED', 'CANCELLED') AND service_ended_at IS NOT NULL)
        OR status NOT IN ('SKIPPED', 'CANCELLED')
    )
);

CREATE TABLE IF NOT EXISTS executor_states (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    queue_session_id INTEGER NOT NULL REFERENCES queue_sessions(id) ON DELETE CASCADE ON UPDATE CASCADE,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE ON UPDATE CASCADE,
    is_ready BOOLEAN NOT NULL DEFAULT FALSE,
    current_ticket_id INTEGER NULL UNIQUE REFERENCES tickets(id) ON DELETE SET NULL ON UPDATE CASCADE,
    last_status_change TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_executor_states_session_user UNIQUE (queue_session_id, user_id),
    CONSTRAINT chk_executor_states_ready_only_without_ticket CHECK (
        NOT (is_ready = TRUE AND current_ticket_id IS NOT NULL)
    )
);

CREATE TABLE IF NOT EXISTS event_logs (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    queue_session_id INTEGER REFERENCES queue_sessions(id) ON DELETE CASCADE ON UPDATE CASCADE,
    ticket_id INTEGER NULL REFERENCES tickets(id) ON DELETE SET NULL ON UPDATE CASCADE,
    actor_user_id INTEGER NULL REFERENCES users(id) ON DELETE SET NULL ON UPDATE CASCADE,
    event_type VARCHAR(100) NOT NULL,
    "timestamp" TIMESTAMP NOT NULL DEFAULT NOW(),
    details JSONB NULL
);

-- =========================
-- INDEXES
-- =========================
CREATE INDEX IF NOT EXISTS idx_user_login ON users(login);
CREATE INDEX IF NOT EXISTS idx_usersession_token ON user_sessions(token);
CREATE INDEX IF NOT EXISTS idx_usersession_user ON user_sessions(user_id, is_active);
CREATE INDEX IF NOT EXISTS idx_usersession_expires ON user_sessions(expires_at);
CREATE INDEX IF NOT EXISTS idx_userrole_user ON user_roles(user_id);
CREATE INDEX IF NOT EXISTS idx_userrole_role ON user_roles(role_id);
CREATE INDEX IF NOT EXISTS idx_clientsession_active ON client_sessions(device_fingerprint, is_active);
CREATE INDEX IF NOT EXISTS idx_servicetype_queue ON service_types(queue_config_id, is_active, sort_order);
CREATE INDEX IF NOT EXISTS idx_session_queue_status ON queue_sessions(queue_config_id, status);
CREATE INDEX IF NOT EXISTS idx_ticket_queue_sort
    ON tickets(queue_session_id, status, priority_level DESC, sort_order ASC, created_at ASC);
CREATE INDEX IF NOT EXISTS idx_ticket_client_session ON tickets(client_session_id, status);
CREATE INDEX IF NOT EXISTS idx_ticket_status_time ON tickets(queue_session_id, status, created_at);
CREATE INDEX IF NOT EXISTS idx_ticket_service_type ON tickets(queue_session_id, service_type_id, status);
CREATE INDEX IF NOT EXISTS idx_ticket_served_agg
    ON tickets(queue_session_id, status)
    INCLUDE (service_started_at, service_ended_at)
    WHERE status = 'SERVED';
CREATE INDEX IF NOT EXISTS idx_executor_ready
    ON executor_states(queue_session_id, is_ready)
    WHERE is_ready = TRUE;
CREATE INDEX IF NOT EXISTS idx_eventlog_session_time ON event_logs(queue_session_id, "timestamp");
CREATE INDEX IF NOT EXISTS idx_eventlog_ticket ON event_logs(ticket_id);
CREATE INDEX IF NOT EXISTS idx_eventlog_type ON event_logs(event_type);

-- only one OPEN session per queue_config
CREATE UNIQUE INDEX IF NOT EXISTS uq_queue_sessions_one_open_per_config
    ON queue_sessions(queue_config_id)
    WHERE status = 'OPEN';

-- only one active ticket per client_session for WAITING/CALLED
CREATE UNIQUE INDEX IF NOT EXISTS uq_tickets_one_active_per_client_session
    ON tickets(client_session_id)
    WHERE client_session_id IS NOT NULL
      AND status IN ('WAITING', 'CALLED');

-- =========================
-- TRIGGERS
-- =========================
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_users_set_updated_at ON users;
CREATE TRIGGER trg_users_set_updated_at
BEFORE UPDATE ON users
FOR EACH ROW
EXECUTE FUNCTION set_updated_at();

DROP TRIGGER IF EXISTS trg_tickets_set_updated_at ON tickets;
CREATE TRIGGER trg_tickets_set_updated_at
BEFORE UPDATE ON tickets
FOR EACH ROW
EXECUTE FUNCTION set_updated_at();

CREATE OR REPLACE FUNCTION set_last_activity_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.last_activity_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_user_sessions_set_last_activity_at ON user_sessions;
CREATE TRIGGER trg_user_sessions_set_last_activity_at
BEFORE UPDATE ON user_sessions
FOR EACH ROW
EXECUTE FUNCTION set_last_activity_at();

-- =========================
-- SEED ROLES
-- =========================
INSERT INTO roles (name, code, description)
VALUES
    ('Администратор', 'ADMIN', 'Управляет конфигурацией очереди, ролями и запуском сессий'),
    ('Оператор', 'OPERATOR', 'Управляет потоком очереди, вызывает, перемещает и отменяет клиентов'),
    ('Исполнитель услуги', 'EXECUTOR', 'Работает с вызванным клиентом и переводит его по статусам обслуживания')
ON CONFLICT (code) DO NOTHING;

-- =========================
-- SEED DEFAULT QUEUE CONFIG + BASE SERVICE TYPE
-- =========================
-- Базовый пользователь-администратор для создания конфигурации (если ещё не существует)
INSERT INTO users (login, password_hash, full_name, last_name, email, is_active)
VALUES
    ('admin', '$2a$10$placeholder_hash_change_me', 'Администратор', 'Системный', 'admin@local.local', TRUE)
ON CONFLICT (login) DO NOTHING;

INSERT INTO queue_configs (name, description, distribution_mode, is_service_type_enabled, is_priority_enabled, created_by_id)
SELECT 'Основная очередь', 'Очередь по умолчанию', 'MANUAL', FALSE, TRUE,
       (SELECT id FROM users WHERE login = 'admin')
WHERE NOT EXISTS (SELECT 1 FROM queue_configs WHERE name = 'Основная очередь');

INSERT INTO service_types (queue_config_id, name, code, letter, base_priority_level, plan_avg_service_time_sec, is_active, sort_order)
SELECT
    (SELECT id FROM queue_configs WHERE name = 'Основная очередь'),
    'Базовая услуга', 'BASE', 'А', 0, 300, TRUE, 0
WHERE NOT EXISTS (SELECT 1 FROM service_types WHERE code = 'BASE');
