# Запуск проекта локально

## Способы запуска

Проект поддерживает два режима запуска:

| Режим | Описание | Когда использовать |
|-------|----------|-------------------|
| **Production** | Бэкенд раздаёт статические сборки фронтендов | Интеграционное тестирование, демо |
| **Development** | Vite dev server с hot-reload для фронтендов | Активная разработка фронтенда |

---

## Production mode (бэкенд раздаёт статику)

В этом режиме ASP.NET Core бэкенд на порту `8080` сам отдаёт статические сборки Vue.js приложений.

### Через Docker (рекомендуется)

```bash
# Сборка и запуск всех сервисов
docker compose up --build backend db
```

После запуска:

| URL | Назначение |
|-----|------------|
| `http://localhost:8080/` | Редирект на `/client/` |
| `http://localhost:8080/client/` | Интерфейс посетителя (получение талона) |
| `http://localhost:8080/app/` | Интерфейс сотрудников (редирект на `/app/login`) |
| `http://localhost:8080/app/login` | Вход для сотрудников |
| `http://localhost:8080/app/dashboard` | Дашборд сотрудника |
| `http://localhost:8080/swagger` | Swagger UI |
| `http://localhost:8080/healthz` | Health check |

### Локально без Docker

```bash
# 1. Собрать фронтенды
cd frontend/client/client-interface
npm ci
npm run build:prod

cd ../../user/user-interface
npm ci
npm run build:prod

# 2. Запустить бэкенд (из корня проекта)
cd ../../backend
dotnet run --project src/Api/Api.csproj
```

> **Важно:** Для локального запуска без Docker нужна запущенная PostgreSQL (см. `appsettings.json`).

---

## Development mode (с Vite dev server)

В этом режиме фронтенды запускаются через Vite dev server с hot-reload, а API-запросы проксируются на бэкенд.

### Через Docker

```bash
# Запуск бэкенда + БД + фронтенды
docker compose --profile dev-frontend up --build
```

Или по отдельности:

```bash
# Терминал 1: БД и бэкенд
docker compose up --build db backend

# Терминал 2: Фронтенды
docker compose --profile dev-frontend up
```

После запуска:

| URL | Назначение |
|-----|------------|
| `http://localhost:5173/` | Клиентский интерфейс (Vite, hot-reload) |
| `http://localhost:5174/` | Интерфейс сотрудников (Vite, hot-reload) |
| `http://localhost:8080/swagger` | Swagger UI |
| `http://localhost:8080/healthz` | Health check |

### Локально без Docker

```bash
# Терминал 1: Бэкенд
cd backend
dotnet run --project src/Api/Api.csproj

# Терминал 2: Клиентский интерфейс
cd frontend/client/client-interface
npm ci
npm run dev

# Терминал 3: Интерфейс сотрудников
cd frontend/user/user-interface
npm ci
npm run dev
```

---

## Структура URL

```
http://localhost:8080/
├── /client/          # Статическая сборка client-interface (посетители)
│   ├── /             # Главная страница
│   └── /ticket       # Страница талона (SPA-роутинг)
├── /app/             # Статическая сборка user-interface (сотрудники)
│   ├── /login        # Вход
│   ├── /dashboard    # Дашборд
│   ├── /operator     # Панель оператора
│   ├── /executor     # Панель исполнителя
│   └── /admin        # Панель администратора
├── /api/             # API endpoints
├── /swagger          # Swagger UI
└── /healthz          # Health check
```

## Переменные окружения

### Бэкенд

| Переменная | Описание | По умолчанию |
|-----------|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Окружение (`Development`/`Production`) | `Development` |
| `ConnectionStrings__DefaultConnection` | Строка подключения к БД | `Host=localhost;Port=5432;...` |

### Фронтенды (только для dev-режима)

| Переменная | Описание | По умолчанию |
|-----------|----------|-------------|
| `VITE_API_BASE_URL` | Базовый URL API | `http://localhost:8080` |

В production-режиме фронтенды ходят в API по относительному пути `/api`, так как раздаются тем же хостом.

## Сборка фронтендов

```bash
# Production-сборка (base path: /client/ или /app/)
npm run build:prod

# Development-сборка (base path: /)
npm run build
```

Разница между `build` и `build:prod`:

| Скрипт | Base path | Назначение |
|--------|-----------|------------|
| `build` | `/` | Для dev-сервера |
| `build:prod` | `/client/` или `/app/` | Для раздачи через бэкенд |