# Логическая модель данных
## Virtual Queue Management System

*Версия: 1.2 | Обновлено: 31.03.2026*

---

## Оглавление

- [Логическая модель данных](#логическая-модель-данных)
  - [Virtual Queue Management System](#virtual-queue-management-system)
  - [Оглавление](#оглавление)
  - [1. Обзор модели](#1-обзор-модели)
  - [2. Сущности](#2-сущности)
    - [2.1. User (Пользователь системы)](#21-user-пользователь-системы)
    - [2.2. Role (Роль)](#22-role-роль)
    - [2.3. UserRole (Связь Пользователь-Роль)](#23-userrole-связь-пользователь-роль)
    - [2.4. QueueConfig (Конфигурация очереди)](#24-queueconfig-конфигурация-очереди)
    - [2.5. QueueSession (Сессия очереди)](#25-queuesession-сессия-очереди)
    - [2.6. Ticket (Талон / Запись в очередь)](#26-ticket-талон--запись-в-очередь)
    - [2.7. ServiceType (Тип обслуживания)](#27-servicetype-тип-обслуживания)
    - [2.8. ExecutorState (Состояние исполнителя)](#28-executorstate-состояние-исполнителя)
    - [2.9. ClientSession (Сессия клиента)](#29-clientsession-сессия-клиента)
    - [2.10. EventLog (Журнал событий)](#210-eventlog-журнал-событий)
  - [3. Связи между сущностями](#3-связи-между-сущностями)
    - [3.1. Диаграмма связей](#31-диаграмма-связей)
    - [3.2. Таблица кардинальностей](#32-таблица-кардинальностей)
  - [4. Бизнес-правила и ограничения](#4-бизнес-правила-и-ограничения)
    - [4.1. Управление позицией в очереди (sort\_order)](#41-управление-позицией-в-очереди-sort_order)
    - [4.2. Добавление позиций в очереди](#42-добавление-позиций-в-очереди)
    - [4.3. Приоритетность](#43-приоритетность)
    - [4.4. Статусы талона (Lifecycle)](#44-статусы-талона-lifecycle)
    - [4.5. Конкурентный доступ (Optimistic Locking)](#45-конкурентный-доступ-optimistic-locking)
    - [4.6. Ограничения по сессии клиента](#46-ограничения-по-сессии-клиента)
    - [4.7. Расчёт времени ожидания](#47-расчёт-времени-ожидания)
    - [4.8. Безопасность данных](#48-безопасность-данных)
  - [5. Приложения](#5-приложения)
    - [A. Перечисляемые типы (Enums)](#a-перечисляемые-типы-enums)
    - [B. Сводная таблица внешних ключей](#b-сводная-таблица-внешних-ключей)
    - [C. Рекомендуемые индексы PostgreSQL](#c-рекомендуемые-индексы-postgresql)
    - [D. Создание ENUM типов](#d-создание-enum-типов)

---

## 1. Обзор модели

Данная логическая модель данных разработана для системы управления виртуальной очередью. Модель является детализацией концептуальной модели

**Ключевые архитектурные решения:**
- Разделение конфигурации очереди (`QueueConfig`) и сессии (`QueueSession`) для хранения истории
- Приоритет определяется типом услуги (`ServiceType`), а не выбирается клиентом
- Позиция в очереди через `sort_order` (DECIMAL) для O(1) перемещения
- Оптимистичная блокировка через `version` для конкурентного доступа
- Аудит всех событий через `EventLog`

---

## 2. Сущности

### 2.1. User (Пользователь системы)

**Назначение:** Хранение учётных данных сотрудников (Администраторы, Операторы, Исполнители).

| Атрибут | Тип данных | Ограничения | Описание |
|---------|------------|-------------|----------|
| `id` | `INTEGER` | `PK`, `AUTO_INCREMENT` | Уникальный идентификатор пользователя |
| `login` | `VARCHAR(100)` | `NOT NULL`, `UNIQUE` | Логин для входа в систему |
| `password_hash` | `VARCHAR(255)` | `NOT NULL` | Хешированный пароль (bcrypt/argon2) |
| `full_name` | `VARCHAR(255)` | `NOT NULL` | Полное имя сотрудника |
| `email` | `VARCHAR(255)` | `NULL`, `UNIQUE` | Контактный email |
| `is_active` | `BOOLEAN` | `NOT NULL`, `DEFAULT true` | Флаг активности учётной записи |
| `created_at` | `TIMESTAMP` | `NOT NULL`, `DEFAULT NOW()` | Дата создания записи |
| `updated_at` | `TIMESTAMP` | `NULL`, `DEFAULT NOW()` | Дата последнего обновления |

**Ключи:**
- Первичный ключ: `id`
- Уникальные: `login`, `email`

**Индексы:**
- `idx_user_login` (`login`) — для быстрого поиска при авторизации

---

### 2.2. Role (Роль)

**Назначение:** Справочник системных ролей. Определяет права доступа к функциям системы.

| Атрибут | Тип данных | Ограничения | Описание |
|---------|------------|-------------|----------|
| `id` | `INTEGER` | `PK`, `AUTO_INCREMENT` | Уникальный идентификатор роли |
| `name` | `VARCHAR(100)` | `NOT NULL`, `UNIQUE` | Отображаемое название роли |
| `code` | `VARCHAR(50)` | `NOT NULL`, `UNIQUE` | Системный код для проверки прав |
| `description` | `TEXT` | `NULL` | Описание полномочий роли |
| `created_at` | `TIMESTAMP` | `NOT NULL`, `DEFAULT NOW()` | Дата создания роли |

**Ключи:**
- Первичный ключ: `id`
- Уникальные: `name`, `code`

**Предустановленные роли:**
- `ADMIN` — Администратор системы
- `OPERATOR` — Оператор очереди
- `EXECUTOR` — Исполнитель услуги

---

### 2.3. UserRole (Связь Пользователь-Роль)

**Назначение:** Реализация связи «Многие-ко-Многим» между пользователями и ролями.

| Атрибут | Тип данных | Ограничения | Описание |
|---------|------------|-------------|----------|
| `id` | `INTEGER` | `PK`, `AUTO_INCREMENT` | Уникальный идентификатор записи |
| `user_id` | `INTEGER` | `NOT NULL`, `FK -> User(id) ON DELETE CASCADE` | Ссылка на пользователя |
| `role_id` | `INTEGER` | `NOT NULL`, `FK -> Role(id) ON DELETE CASCADE` | Ссылка на роль |
| `assigned_at` | `TIMESTAMP` | `NOT NULL`, `DEFAULT NOW()` | Дата назначения роли |
| `assigned_by` | `INTEGER` | `NULL`, `FK -> User(id) ON DELETE SET NULL` | Кто назначил роль |

**Ключи:**
- Первичный ключ: `id`
- Уникальный составной: `UNIQUE(user_id, role_id)` — запрет дублирования

**Индексы:**
- `idx_userrole_user` (`user_id`) — поиск ролей пользователя
- `idx_userrole_role` (`role_id`) — поиск пользователей по роли

---

### 2.4. QueueConfig (Конфигурация очереди)

**Назначение:** Шаблон очереди с настройками, не меняющимися в рамках сессии.

| Атрибут | Тип данных | Ограничения | Описание |
|---------|------------|-------------|----------|
| `id` | `INTEGER` | `PK`, `AUTO_INCREMENT` | Уникальный идентификатор очереди |
| `name` | `VARCHAR(255)` | `NOT NULL` | Отображаемое название очереди |
| `description` | `TEXT` | `NULL` | Подробное описание очереди |
| `distribution_mode` | `distribution_mode (ENUM)` | `NOT NULL`, `DEFAULT 'MANUAL'` | MANUAL или AUTO |
| `is_service_type_enabled` | `BOOLEAN` | `NOT NULL`, `DEFAULT false` | Требовать ли выбор услуги |
| `is_priority_enabled` | `BOOLEAN` | `NOT NULL`, `DEFAULT true` | Разрешено ли приоритетное обслуживание |
| `is_active` | `BOOLEAN` | `NOT NULL`, `DEFAULT true` | Флаг активности конфигурации |
| `created_at` | `TIMESTAMP` | `NOT NULL`, `DEFAULT NOW()` | Дата создания конфигурации |
| `created_by_id` | `INTEGER` | `NOT NULL`, `FK -> User(id) ON DELETE RESTRICT` | Администратор-создатель |

**Ключи:**
- Первичный ключ: `id`
- Внешние ключи: `created_by -> User(id)`

**Типы distribution_mode:**
- `MANUAL` - Оператор вызывает клиентов вручную
- `AUTO` - Система автоматически назначает готовым исполнителям

**Проверочные ограничения:**
```
CHECK (distribution_mode IN ('MANUAL', 'AUTO'))
```

---

### 2.5. QueueSession (Сессия очереди)

**Назначение:** Конкретный запуск очереди во времени. Позволяет хранить историю работы.

| Атрибут | Тип данных | Ограничения | Описание |
|---------|------------|-------------|----------|
| `id` | `INTEGER` | `PK`, `AUTO_INCREMENT` | Уникальный идентификатор сессии |
| `queue_config_id` | `INTEGER` | `NOT NULL`, `FK -> QueueConfig(id) ON DELETE CASCADE` | Ссылка на конфигурацию |
| `status` | `session_status (ENUM)` | `NOT NULL`, `DEFAULT 'DRAFT'` | DRAFT, OPEN, PAUSED, CLOSED |
| `started_at` | `TIMESTAMP` | `NULL` | Фактическое время начала работы |
| `closed_at` | `TIMESTAMP` | `NULL` | Время завершения сессии |
| `current_ticket_number` | `INTEGER` | `NOT NULL`, `DEFAULT 0`, `CHECK (>= 0)` | Счётчик для генерации талонов |
| `served_count` | `INTEGER` | `NOT NULL`, `DEFAULT 0`, `CHECK (>= 0)` | Кэш. Число обслуженных клиентов |
| `total_service_time_sec` | `INTEGER` | `NOT NULL`, `DEFAULT 0`, `CHECK (>= 0)` | Кэш. Сумма времени обслуживаний (сек) |
| `created_by` | `INTEGER` | `NOT NULL`, `FK -> User(id) ON DELETE RESTRICT` | Администратор, запустивший сессию |
| `created_at` | `TIMESTAMP` | `NOT NULL`, `DEFAULT NOW()` | Дата создания сессии |

**Ключи:**
- Первичный ключ: `id`
- Внешние ключи: `queue_config_id -> QueueConfig(id)`, `created_by -> User(id)`

**Типы status:**
- `DRAFT` - Черновик, не активна
- `OPEN` - Активна, принимает клиентов
- `PAUSED` - На паузе, не принимает новых
- `CLOSED` - Завершена

Вычисляемые значения:
- `served_count` - Кэш. Число обслуженных клиентов
- `total_service_time_sec` - Кэш. Сумма времени обслуживаний (сек)
- `avg_service_time_actual` - Вычисляемое. Фактическое среднее время обслуживания (мин)

**Индексы:**
- `idx_session_queue_status` (`queue_config_id`, `status`) — поиск активных сессий

**Проверочные ограничения:**
```
CHECK (status IN ('DRAFT', 'OPEN', 'PAUSED', 'CLOSED'))
CHECK (closed_at IS NULL OR closed_at >= started_at)
```

**Бизнес-правила:**
- Только одна сессия со статусом `OPEN` может быть активна для одной `queue_config_id`
- `served_count`, `total_service_time_sec` обновляются атомарно вместе с изменением статуса Ticket на SERVED
- `avg_service_time_actual` рассчитывается: `total_service_time_sec / served_count / 60`

---

### 2.6. Ticket (Талон / Запись в очередь)

**Назначение:** Ключевая сущность системы. Представляет клиента в очереди.

| Атрибут | Тип данных | Ограничения | Описание |
|---------|------------|-------------|----------|
| `id` | `INTEGER` | `PK`, `AUTO_INCREMENT` | Уникальный идентификатор талона |
| `queue_session_id` | `INTEGER` | `NOT NULL`, `FK -> QueueSession(id) ON DELETE CASCADE` | Ссылка на сессию |
| `service_type_id` | `INTEGER` | `NULL`, `FK -> ServiceType(id) ON DELETE SET NULL` | Ссылка на тип услуги |
| `ticket_number` | `VARCHAR(20)` | `NOT NULL` | Видимый номер (напр. «А-005») |
| `client_name` | `VARCHAR(100)` | `NOT NULL` | Имя клиента |
| `client_surname` | `VARCHAR(100)` | `NOT NULL` | Фамилия клиента |
| `sort_order` | `DECIMAL(20,10)` | `NOT NULL`, `CHECK (>= 0)` | Позиция для сортировки в очереди |
| `priority_level` | `INTEGER` | `NOT NULL`, `DEFAULT 0`, `CHECK (>= 0)` | Текущий приоритет данного клиента. Изначально соответствует приоритету типа обслуживания |
| `status` | `ticket_status (ENUM)` | `NOT NULL`, `DEFAULT 'WAITING'` | Текущий статус талона |
| `version` | `INTEGER` | `NOT NULL`, `DEFAULT 1`, `CHECK (>= 1)` | Для оптимистичной блокировки |
| `created_at` | `TIMESTAMP` | `NOT NULL`, `DEFAULT NOW()` | Время записи |
| `called_at` | `TIMESTAMP` | `NULL` | Время вызова |
| `service_started_at` | `TIMESTAMP` | `NULL` | Начало обслуживания |
| `service_ended_at` | `TIMESTAMP` | `NULL` | Завершение обслуживания |
| `updated_at` | `TIMESTAMP` | `NULL`, `DEFAULT NOW()` | Дата последнего изменения |
| `served_by_user_id` | `INTEGER` | `NULL`, `FK -> User(id) ON DELETE SET NULL` | Исполнитель |
| `client_session_id` | `INTEGER` | `NULL`, `FK -> ClientSession(id) ON DELETE SET NULL` | Сессия устройства |
| `cancel_reason` | `TEXT` | `NULL` | Причина отмены/пропуска |

**Ключи:**
- Первичный ключ: `id`
- Внешние ключи: `queue_session_id`, `service_type_id`, `served_by_user_id`, `client_session_id`
- Уникальный составной: `UNIQUE(queue_session_id, ticket_number)` — запрет дублирования

**Типы status:**
- `WAITING` - Ожидает вызова
- `CANCELLED` - Отменён клиентом или оператором
- `CALLED` - Вызван, ожидает подтверждения
- `SERVING` - Обслуживается
- `SERVED` - Обслужен успешно
- `SKIPPED` - Пропущен (не явился)

**Индексы:**
```
idx_ticket_queue_sort     (queue_session_id, status, priority_level DESC, sort_order ASC)
idx_ticket_client_session (client_session_id, status)
idx_ticket_status_time    (queue_session_id, status, created_at)
idx_ticket_service_type   (queue_session_id, service_type_id, status)
```

**Проверочные ограничения:**
```
CHECK (status IN ('WAITING', 'CALLED', 'SERVING', 'SERVED', 'SKIPPED', 'CANCELLED'))
CHECK (
  (status = 'SERVED' AND service_ended_at IS NOT NULL) OR
  (status != 'SERVED')
)
CHECK (
  (status IN ('SERVED', 'SKIPPED', 'CANCELLED') AND service_ended_at IS NOT NULL) OR
  (status NOT IN ('SERVED', 'SKIPPED', 'CANCELLED'))
)
```

**Бизнес-правила:**
- `priority_level` копируется из `ServiceType.base_priority_level` выбранного `ServiceType` при создании талона
- Если `service_type_id` не назначен - назначается базовый тип обслуживания, имеющий приоритет 0
- `priority_level` может обновляться при необходимости только при статусе талона `WAITING`
- При создании нового талона с тем же `client_session_id` — предыдущие аннулируются

---

### 2.7. ServiceType (Тип обслуживания)

**Назначение:** Справочник типов услуг. Определяет приоритет и плановое время для каждой услуги.

| Атрибут | Тип данных | Ограничения | Описание |
|---------|------------|-------------|----------|
| `id` | `INTEGER` | `PK`, `AUTO_INCREMENT` | Уникальный идентификатор типа услуги |
| `queue_config_id` | `INTEGER` | `NOT NULL`, `FK -> QueueConfig(id) ON DELETE CASCADE` | Ссылка на конфигурацию |
| `name` | `VARCHAR(255)` | `NOT NULL` | Название услуги |
| `code` | `VARCHAR(50)` | `NOT NULL`, `UNIQUE` | Системный код |
| `letter` | `CHAR(1)` | `NOT NULL` | Буква для номера талона |
| `base_priority_level` | `INTEGER` | `NOT NULL`, `DEFAULT 0`, `CHECK (>= 0)` | Базовый приоритет услуги |
| `plan_avg_service_time` | `INTEGER` | `NULL`, `CHECK (> 0)` | Плановое время (секунды) |
| `is_active` | `BOOLEAN` | `NOT NULL`, `DEFAULT true` | Активен ли тип услуги |
| `is_highlighting` | `BOOLEAN` | `NOT NULL`, `DEFAULT false` | Выделяется ли в UI |
| `sort_order` | `INTEGER` | `NOT NULL`, `DEFAULT 0` | Порядок отображения |
| `created_at` | `TIMESTAMP` | `NOT NULL`, `DEFAULT NOW()` | Дата создания |

**Ключи:**
- Первичный ключ: `id`
- Уникальные: `code`
- Внешние ключи: `queue_config_id -> QueueConfig(id)`

**Индексы:**
- `idx_servicetype_queue` (`queue_config_id`, `is_active`, `sort_order`) — для списка услуг

**Бизнес-правила:**
- Если `QueueConfig.is_service_type_enabled = false`, используется базовая услуга по умолчанию
- Приоритет талона `Ticket.priority_level` определяется приоритетом выбранной услуги `ServiceType.base_priority_level`. Базовая услуга имеет приоритет 0

---

### 2.8. ExecutorState (Состояние исполнителя)

**Назначение:** Хранит состояние готовности исполнителя в рамках конкретной сессии очереди.

| Атрибут | Тип данных | Ограничения | Описание |
|---------|------------|-------------|----------|
| `id` | `INTEGER` | `PK`, `AUTO_INCREMENT` | Уникальный идентификатор записи |
| `queue_session_id` | `INTEGER` | `NOT NULL`, `FK -> QueueSession(id) ON DELETE CASCADE` | Ссылка на сессию |
| `user_id` | `INTEGER` | `NOT NULL`, `FK -> User(id) ON DELETE CASCADE` | Исполнитель услуги |
| `is_ready` | `BOOLEAN` | `NOT NULL`, `DEFAULT false` | Флаг готовности |
| `current_ticket_id` | `INTEGER` | `NULL`, `FK -> Ticket(id) ON DELETE SET NULL`, `UNIQUE` | Текущий талон |
| `last_status_change` | `TIMESTAMP` | `NOT NULL`, `DEFAULT NOW()` | Время последнего изменения |
| `served_count` | `INTEGER` | `NOT NULL`, `DEFAULT 0`, `CHECK (>= 0)` | Счётчик обслуженных за сессию |

**Ключи:**
- Первичный ключ: `id`
- Уникальный составной: `UNIQUE(queue_session_id, user_id)`
- Внешние ключи: `queue_session_id`, `user_id`, `current_ticket_id`

**Индексы:**
- `idx_executor_ready` (`queue_session_id`, `is_ready`) WHERE `is_ready = true` — поиск свободных

Вычисляемые значения:
- `served_count` - Кэш. Счётчик обслуженных за сессию

**Бизнес-правила:**
- Один исполнитель может иметь лишь одну запись на сессию
- `current_ticket_id` заполняется только при статусе `SERVING`
- `served_count` обновляется атомарно вместе с изменением статуса Ticket, имеющим данного исполнителя, на SERVED

---

### 2.9. ClientSession (Сессия клиента)

**Назначение:** Отслеживает сессию браузера/устройства клиента. Реализация требования «один активный талон с устройства».

| Атрибут | Тип данных | Ограничения | Описание |
|---------|------------|-------------|----------|
| `id` | `INTEGER` | `PK`, `AUTO_INCREMENT` | Уникальный идентификатор сессии |
| `device_fingerprint` | `VARCHAR(255)` | `NOT NULL` | Идентификатор устройства/браузера |
| `created_at` | `TIMESTAMP` | `NOT NULL`, `DEFAULT NOW()` | Время создания сессии |
| `expires_at` | `TIMESTAMP` | `NOT NULL`, `DEFAULT (NOW() + INTERVAL '24 hours')` | Время истечения сессии |
| `is_active` | `BOOLEAN` | `NOT NULL`, `DEFAULT true` | Флаг активности сессии |
| `ip_address` | `VARCHAR(45)` | `NULL` | IP-адрес клиента (IPv6 compatible) |
| `user_agent` | `TEXT` | `NULL` | Информация о браузере/устройстве |

**Ключи:**
- Первичный ключ: `id`

**Индексы:**
- `idx_clientsession_active` (`device_fingerprint`, `is_active`) — поиск активной сессии

**Бизнес-правила:**
- Сессия считается неактивной после `expires_at`
- При создании нового талона все активные талоны с этим `client_session_id` аннулируются. За исключением талонов в статусе `SERVING`, `SERVED` (логировать предупреждение)

---

### 2.10. EventLog (Журнал событий)

**Назначение:** Хранит историю всех значимых событий в системе. Используется для аудита, аналитики и отладки.

| Атрибут | Тип данных | Ограничения | Описание |
|---------|------------|-------------|----------|
| `id` | `INTEGER` | `PK`, `AUTO_INCREMENT` | Уникальный идентификатор события |
| `queue_session_id` | `INTEGER` | `NOT NULL`, `FK -> QueueSession(id) ON DELETE CASCADE` | Контекст сессии |
| `ticket_id` | `INTEGER` | `NULL`, `FK -> Ticket(id) ON DELETE SET NULL` | Связанный талон |
| `actor_user_id` | `INTEGER` | `NULL`, `FK -> User(id) ON DELETE SET NULL` | Кто совершил (или SYSTEM) |
| `event_type` | `VARCHAR(100)` | `NOT NULL` | Тип события |
| `timestamp` | `TIMESTAMP` | `NOT NULL`, `DEFAULT NOW()` | Дата и время события |
| `details` | `JSONB` | `NULL` | Дополнительные данные (JSON) |

**Ключи:**
- Первичный ключ: `id`
- Внешние ключи: `queue_session_id`, `ticket_id`, `actor_user_id`

**Индексы:**
```
idx_eventlog_session_time (queue_session_id, timestamp)  — фильтрация по времени
idx_eventlog_ticket      (ticket_id)               — история талона
idx_eventlog_type        (event_type)              — аналитика по типам
```

**Типы событий:**  
Описаны в [приложении](#a-перечисляемые-типы-enums)

---

## 3. Связи между сущностями

### 3.1. Диаграмма связей

### 3.2. Таблица кардинальностей

| Сущность 1 | Связь | Сущность 2 | Кардинальность | Правило ON DELETE |
|------------|-------|------------|----------------|-------------------|
| User | имеет | UserRole | 1 : N | CASCADE |
| UserRole | относится к | Role | N : 1 | CASCADE |
| QueueConfig | имеет сессии | QueueSession | 1 : N | RESTRICT (created_by) |
| QueueConfig | имеет типы услуг | ServiceType | 1 : N | CASCADE |
| QueueSession | содержит | Ticket | 1 : N | CASCADE |
| QueueSession | имеет статусы исполнителей | ExecutorState | 1 : N | CASCADE |
| Ticket | принадлежит сессии | ClientSession | N : 1 | SET NULL |
| Ticket | обслуживается | User | N : 1 | SET NULL |
| Ticket | имеет тип услуги | ServiceType | N : 1 | SET NULL |
| ExecutorState | обслуживает в данный момент | Ticket | 1 : 1 | SET NULL |
| QueueSession/Ticket/User | генерирует | EventLog | 1 : N | CASCADE/SET NULL |

---

## 4. Бизнес-правила и ограничения

### 4.1. Управление позицией в очереди (sort_order)

Атрибут sort_order использует десятичные числа с шагом 1000  

Позиция в очереди определяется путём сортировки всех активных талонов в состоянии ожидания:
  1. Сначала по приоритету (priority_level DESC)
  2. Затем по полю sort_order (ASC)
  3. При равенстве — по времени создания (created_at ASC)

При добавлении нового талона в очередь, его sort_order рассчитывается на основании максимального sort_order среди талонов с таким же приоритетом  

```sql
ORDER BY priority_level DESC, sort_order ASC, created_at ASC
```

Данный подход в дальнейшем позволяет добиться O(1) перемещение без пересчёта всех записей. При перемещении sort_order конкретного талона пересчитывается как среднее между sort_order соседних талонов. Тем самым при сортировке клиент будет находится в необходимом месте  

Ренормализация выполняется в фоновом режиме когда минимальный интервал < 100. Событие логируется в EventLog как `QUEUE_RENORMALIZED`


---

### 4.2. Добавление позиций в очереди

Назначение sort_order новому клиенту происходит на основании последнего клиента с таким же приоритетом в очереди. Отображение очереди в таком случае остаётся корректным даже при дублировании порядка (sort_order) среды разных групп приоритетов, так как среди текущей группы приоритетов этот порядок всё ещё остаётся уникален.

---

### 4.3. Приоритетность

Если конфигурацией очереди клиенту не предоставляется выбор типа услуги, то ему автоматически должна присваиваться "базовая услуга", имеющая приоритет 0.  
Если выбор услуги предоставлен, клиент получает приоритет (`Ticket.priority_level`), соответствующий приоритету выбранного типа обслуживания (`ServiceType.base_priority_level`)
При ручном изменении позиции клиента в очереди его приоритет при необходимости должен изменяться в зависимости от новой позиции позиции:
  - Если клиент перемещается в группу клиентов с другим приоритетом, его `Ticket.priority_level` обновляется в соответствии с целевой позицией
  - Изменение приоритета логируется в EventLog как PRIORITY_CHANGED
  - Оба изменения (sort_order + priority_level) выполняются в одной транзакции

---

### 4.4. Статусы талона (Lifecycle)

```
┌──────────┐    вызов    ┌──────────┐   начало    ┌───────────┐
│ WAITING  │ ──────────> │ CALLED   │ ──────────> │ SERVING   │
└──────────┘             └──────────┘             └───────────┘
     │                        │                        │
     │ отмена                 │ пропуск                │ завершение
     ▼                        ▼                        ▼
┌──────────┐             ┌──────────┐             ┌───────────┐
│CANCELLED │             │ SKIPPED  │             │ SERVED    │
└──────────┘             └──────────┘             └───────────┘
```

**Ограничения переходов:**

| Из статуса | В статус | Условие |
|------------|----------|---------|
| WAITING | CALLED | Оператор или автоматика |
| WAITING | CANCELLED | Клиент или Оператор |
| CALLED | SERVING | Исполнитель подтвердил |
| CALLED | SKIPPED | Клиент не явился |
| SERVING | SERVED | Обслуживание завершено |

**Обязательные поля при переходе:**
- в `SERVED` -> `service_ended_at` NOT NULL
- в `SKIPPED` -> `service_ended_at` NOT NULL
- в `CANCELLED` -> `service_ended_at` NOT NULL

---

### 4.5. Конкурентный доступ (Optimistic Locking)

Поле `Ticket.version` увеличивается при каждом обновлении  
Проверка выполняется через `UPDATE Ticket SET version = version + 1 WHERE id = ? AND version = ?`  
При конфликте вернуть ошибку 409 Conflict, запросить обновление данных

В дальнейшем это должно позволить предусмотреть следующие сценарии защиты:
- Оператор vs Оператор (одновременное перемещение)
- Оператор vs Клиент (вызов vs отмена)
- Авто vs Оператор (автоматическое распределение vs ручной вызов)
- Исполнитель vs Исполнитель (двойное назначение)

---

### 4.6. Ограничения по сессии клиента

Согласно функциональным требованиям установлено правило - один активный талон на одну ClientSession  
При создании нового талона необходимо:
  1. Найти все активные талоны с этим client_session_id
  2. Перевести их в статус CANCELLED
  3. Создать новый талон
  4. Записать событие в EventLog

Исключение: Талоны в статусе `SERVING`, `SERVED` не аннулируются (логировать предупреждение)

---

### 4.7. Расчёт времени ожидания

Время ожидания каждого клиента рассчитывается как время_ожидания = (`людей_передо_мной` × `среднее_время_обслуживания`) / `активных_исполнителей`.  
Где:
  - людей_передо_мной = COUNT(Ticket WHERE status='WAITING' и позиция < текущей)
  - среднее_время_обслуживания (`avg_service_time_actual`) = `QueueSession.total_service_time_sec` / `QueueSession.served_count`
  - активных_исполнителей = COUNT(ExecutorState WHERE queue_session_id=? AND (is_ready = true OR current_ticket_id IS NOT NULL))
    (*число исполнителей, обслуживающих клиентов в данный момент ИЛИ готовых к обслуживанию*)

До момента завершения обслуживания первого клиента в качестве среднего времени обслуживания используется плановое среднее временя обслуживания каждого клиента в очереди `ServiceType.plan_avg_service_time`, зависящего от типа обслуживания.  

Если активных_исполнителей = 0, отображать время ожидания для одного активного исполнителя  
Кратковременные колебания времени между завершением обслуживания и нажатием «Готов» допустимы  
Обновление `QueueSession.total_service_time_sec` и `QueueSession.served_count` производится автоматически после каждого нового обслуживания  

---

### 4.8. Безопасность данных

| Требование | Реализация |
|------------|------------|
| Пароли | Хеширование bcrypt/argon2 (NFR-S-01) |
| Передача | HTTPS обязательно (NFR-S-02) |
| SQL Injection | Параметризированные запросы (NFR-S-03) |
| XSS | Санитизация всех входных данных (NFR-S-03) |
| Audit Trail | Все действия в EventLog (3.8) |

---

## 5. Приложения

### A. Перечисляемые типы (Enums)

**Ticket.status:**
| Значение | Описание |
|----------|----------|
| `WAITING` | Ожидает вызова |
| `CALLED` | Вызван, ожидает подтверждения |
| `SERVING` | Обслуживается |
| `SERVED` | Обслужен успешно |
| `SKIPPED` | Пропущен (не явился) |
| `CANCELLED` | Отменён клиентом или оператором |

**QueueSession.status:**
| Значение | Описание |
|----------|----------|
| `DRAFT` | Черновик, не активна |
| `OPEN` | Активна, принимает клиентов |
| `PAUSED` | На паузе, не принимает новых |
| `CLOSED` | Завершена |

**QueueConfig.distribution_mode:**
| Значение | Описание |
|----------|----------|
| `MANUAL` | Оператор вызывает клиентов вручную |
| `AUTO` | Система автоматически назначает готовым исполнителям |

**EventLog.event_type:**
| Значение | Описание |
|----------|----------|
| `TICKET_CREATED` | Создание талона |
| `TICKET_CALLED` | Вызов клиента |
| `SERVICE_STARTED` | Начало обслуживания |
| `SERVICE_SERVED` | Обслуживание завершено успешно |
| `SERVICE_SKIPPED` | Клиент пропущен (не явился) |
| `TICKET_CANCELLED` | Талон отменён |
| `TICKET_MOVED` | Талон перемещён в очереди |
| `PRIORITY_CHANGED` | Изменён приоритет талона |
| `SESSION_STARTED` | Сессия очереди начата |
| `SESSION_PAUSED` | Сессия приостановлена |
| `SESSION_RESUMED` | Сессия возобновлена |
| `SESSION_CLOSED` | Сессия закрыта |
| `EXECUTOR_READY` | Исполнитель готов к обслуживанию |
| `EXECUTOR_NOT_READY` | Исполнитель не готов |
| `AUTO_ASSIGNMENT` | Автоматическое назначение клиента |
| `AUTO_ASSIGNMENT_FAILED` | Авто-назначение не удалось |
| `QUEUE_RENORMALIZED` | Ренормализация sort_order |

---

### B. Сводная таблица внешних ключей

| Таблица | Поле | Ссылается на | ON DELETE | ON UPDATE |
|---------|------|--------------|-----------|-----------|
| UserRole | user_id | User(id) | CASCADE | CASCADE |
| UserRole | role_id | Role(id) | CASCADE | CASCADE |
| QueueConfig | created_by | User(id) | RESTRICT | CASCADE |
| QueueSession | queue_config_id | QueueConfig(id) | CASCADE | CASCADE |
| QueueSession | created_by | User(id) | RESTRICT | CASCADE |
| Ticket | queue_session_id | QueueSession(id) | CASCADE | CASCADE |
| Ticket | service_type_id | ServiceType(id) | SET NULL | CASCADE |
| Ticket | served_by_user_id | User(id) | SET NULL | CASCADE |
| Ticket | client_session_id | ClientSession(id) | SET NULL | CASCADE |
| ServiceType | queue_config_id | QueueConfig(id) | CASCADE | CASCADE |
| ExecutorState | queue_session_id | QueueSession(id) | CASCADE | CASCADE |
| ExecutorState | user_id | User(id) | CASCADE | CASCADE |
| ExecutorState | current_ticket_id | Ticket(id) | SET NULL | CASCADE |
| EventLog | queue_session_id | QueueSession(id) | CASCADE | CASCADE |
| EventLog | ticket_id | Ticket(id) | SET NULL | CASCADE |
| EventLog | actor_user_id | User(id) | SET NULL | CASCADE |

---

### C. Рекомендуемые индексы PostgreSQL

```sql
-- Ticket: основной запрос отображения очереди
CREATE INDEX idx_ticket_queue_sort ON Ticket(queue_session_id, status, priority_level, sort_order, created_at);
ORDER BY priority_level DESC, sort_order ASC, created_at ASC

-- Ticket: поиск по сессии клиента
CREATE INDEX idx_ticket_client_session ON Ticket(client_session_id, status);

-- Ticket: аналитика по статусам
CREATE INDEX idx_ticket_status_time ON Ticket(queue_session_id, status, created_at);

-- Ticket: фильтрация по типу услуги
CREATE INDEX idx_ticket_service_type ON Ticket(queue_session_id, service_type_id, status);

-- ExecutorState: поиск готовых исполнителей
CREATE INDEX idx_executor_ready ON ExecutorState(queue_session_id, is_ready) WHERE is_ready = true;

-- EventLog: фильтрация по сессии и времени
CREATE INDEX idx_eventlog_session_time ON EventLog(queue_session_id, timestamp);

-- EventLog: история талона
CREATE INDEX idx_eventlog_ticket ON EventLog(ticket_id);

-- EventLog: аналитика по типам
CREATE INDEX idx_eventlog_type ON EventLog(event_type);

-- ClientSession: поиск по отпечатку устройства
CREATE INDEX idx_clientsession_active ON ClientSession(device_fingerprint, is_active);

-- ServiceType: список услуг очереди
CREATE INDEX idx_servicetype_queue_config ON ServiceType(queue_config_id, is_active, sort_order);

-- QueueSession: поиск активных сессий
CREATE INDEX idx_session_queue_status ON QueueSession(queue_config_id, status);
```

### D. Создание ENUM типов
```sql
CREATE TYPE ticket_status AS ENUM ('WAITING', 'CALLED', 'SERVING', 'SERVED', 'SKIPPED', 'CANCELLED');
CREATE TYPE session_status AS ENUM ('DRAFT', 'OPEN', 'PAUSED', 'CLOSED');
CREATE TYPE distribution_mode AS ENUM ('MANUAL', 'AUTO');
```

---