# Полная документация бэкенда системы

## Обзор

Данная документация предоставляет полное описание реализации бэкенда системы управления виртуальными очередями (VQMS). Документация разделена на три логические части для удобства изучения:

### Части документации

1. **[Часть 1: Обзор архитектуры](backend_documentation_part1_architecture.md)**
2. **[Часть 2: Детали реализации](backend_documentation_part2_implementation.md)**
3. **[Часть 3: API и интеграции](backend_documentation_part3_api_integrations.md)**

## Краткое содержание системы

### Назначение системы
Virtual Queue Management System (VQMS) - это система управления виртуальными очередями, предназначенная для автоматизации процессов обслуживания клиентов в различных организациях (банки, медицинские учреждения, госучреждения).

### Ключевые возможности
1. **Управление очередями**: Создание, настройка и управление виртуальными очередями
2. **Работа с талонами**: Создание, вызов, обслуживание и отмена талонов
3. **Управление пользователями**: Администраторы, операторы, исполнители
4. **Мониторинг**: Отслеживание состояния очереди и исполнителей
5. **Аналитика**: Сбор статистики и метрик производительности

### Технологический стек
- **Бэкенд**: .NET 10.0, ASP.NET Core, C# 12.0
- **База данных**: PostgreSQL 16+, Entity Framework Core 8.0
- **Контейнеризация**: Docker, Docker Compose
- **Логирование**: Serilog
- **Документация API**: Swagger/OpenAPI 3.0
- **События**: MediatR

### Архитектура
Система построена по принципам **Clean Architecture** с четким разделением на слои:
- **Domain Layer**: Бизнес-логика и сущности
- **Application Layer**: Сервисы приложения и DTO
- **Infrastructure Layer**: Работа с БД и внешними сервисами
- **API Layer**: HTTP endpoints и middleware

## Быстрый старт

### Требования
- .NET 10.0 SDK
- PostgreSQL 16+
- Docker и Docker Compose (опционально)

### Запуск в разработке
``` bash
# Клонирование репозитория  
git clone <repository-url>  
cd virtual_queue_management_system  
  
# Запуск через Docker Compose  
docker-compose up -d  
  
# Или запуск вручную  
cd backend  
dotnet restore  
dotnet run --project src/Api/Api.csproj  
```

### Доступ к API
- **API**: http://localhost:8080
- **Swagger UI**: http://localhost:8080/swagger
- **Health Check**: http://localhost:8080/healthz

### Начальные учетные данные
- **Администратор**: login=`admin`, password=`admin123`
- **Оператор**: login=`operator`, password=`operator123`

## Структура проекта

```
backend/  
├── src/  
│   ├── Api/                    # API Layer  
│   │   ├── Endpoints/         # Minimal API endpoints  
│   │   ├── Middleware/        # Authentication middleware  
│   │   ├── Program.cs         # Точка входа  
│   │   └── appsettings.json   # Конфигурация  
│   ├── Application/           # Application Layer  
│   │   ├── Services/          # Сервисы приложения  
│   │   ├── DTOs/              # Data Transfer Objects  
│   │   ├── Events/            # Доменные события  
│   │   └── DependencyInjection/  
│   ├── Domain/                # Domain Layer  
│   │   ├── Entities/          # Доменные сущности  
│   │   ├── Enums/             # Перечисления  
│   │   ├── Interfaces/        # Абстракции  
│   │   └── DTOs/              # Внутренние DTO  
│   └── Infrastructure/        # Infrastructure Layer  
│       ├── Data/              # Работа с БД (DbContext)  
│       ├── Security/          # Сервисы безопасности  
│       └── DependencyInjection/  
├── Dockerfile                 # Конфигурация Docker образа  
└── README.md                  # Документация проекта  
```

## Основные сущности

### 1. User (Пользователь)
Администраторы, операторы и исполнители системы.

### 2. QueueConfig (Конфигурация очереди)
Настройки и параметры очереди (режим распределения, типы услуг и т.д.).

### 3. QueueSession (Сессия очереди)
Рабочая сессия очереди в определенный период времени.

### 4. Ticket (Талон)
Запись клиента в очереди на обслуживание.

### 5. ServiceType (Тип услуги)
Категоризация услуг для талонов.

### 6. ExecutorState (Состояние исполнителя)
Отслеживание состояния оператора/исполнителя.

### 7. ClientSession (Клиентская сессия)
Сессия клиента для отслеживания состояния в системе.

### 8. EventLog (Лог событий)
Аудит всех значимых действий в системе.

## Ключевые API endpoints

### Аутентификация
- `POST /api/auth/login` - Вход в систему
- `POST /api/auth/logout` - Выход из системы
- `GET /api/auth/me` - Информация о текущем пользователе

### Талоны
- `POST /api/tickets` - Создание талона (публичный)
- `GET /api/tickets/me` - Активный талон клиента
- `POST /api/tickets/{id}/call` - Вызов талона
- `POST /api/tickets/{id}/start-service` - Начало обслуживания
- `POST /api/tickets/{id}/complete-service` - Завершение обслуживания
- `GET /api/tickets/queue` - Текущая очередь

### Сессии очереди
- `GET /api/queue-sessions/active` - Активная сессия
- `POST /api/queue-sessions` - Создание сессии
- `POST /api/queue-sessions/{id}/open` - Открытие сессии
- `POST /api/queue-sessions/{id}/close` - Закрытие сессии

### Исполнители
- `GET /api/executor-states` - Состояния исполнителей
- `POST /api/executor-states/ready` - Установка состояния "готов"
- `POST /api/executor-states/take-next` - Взять следующий талон

## Безопасность

### Аутентификация
- Bearer Token аутентификация
- Два типа токенов: User Token и Client Token
- Валидация токенов через кастомный TokenValidationService

### Авторизация
- Ролевая модель (RBAC) с ролями: ADMIN, OPERATOR, EXECUTOR
- Проверка прав на уровне endpoints
- Ресурс-уровневая авторизация для чувствительных операций

### Защита данных
- Хеширование паролей с использованием PBKDF2
- Хеширование токенов для безопасного хранения
- Защита от SQL injection через параметризованные запросы EF Core

## Мониторинг и логирование

### Логирование
- Структурированное логирование через Serilog
- Контекстное обогащение логов
- Вывод в консоль (разработка) и планируется в файлы/Elasticsearch

### Health Checks
- `GET /healthz` - Liveness probe для Kubernetes
- `GET /api/health` - Детальная проверка здоровья
- Проверка подключения к БД и других зависимостей

### Метрики
- Встроенные метрики ASP.NET Core
- Кастомные метрики через доменные события
- Планируется интеграция с Prometheus

## Развертывание

### Docker
``` bash
# Сборка и запуск  
docker-compose up -d  
  
# Остановка  
docker-compose down  
  
# Просмотр логов  
docker-compose logs -f api  
```

### Kubernetes
``` yaml
# Пример deployment  
apiVersion: apps/v1  
kind: Deployment  
metadata:  
  name: vqms-api  
spec:  
  replicas: 3  
  selector:  
    matchLabels:  
      app: vqms-api  
  template:  
    metadata:  
      labels:  
        app: vqms-api  
    spec:  
      containers:  
      - name: api  
        image: vqms-api:latest  
        ports:  
        - containerPort: 8080  
        env:  
        - name: ConnectionStrings__DefaultConnection  
          valueFrom:  
            secretKeyRef:  
              name: vqms-secrets  
              key: database-connection  
        livenessProbe:  
          httpGet:  
            path: /healthz  
            port: 8080  
        readinessProbe:  
          httpGet:  
            path: /api/health  
            port: 8080  
```

### Переменные окружения
``` bash
# Обязательные  
ASPNETCORE_ENVIRONMENT=Production  
ConnectionStrings__DefaultConnection=Host=...;Database=...;Username=...;Password=...  
  
# Опциональные  
Serilog__MinimumLevel=Information  
CORS__AllowedOrigins=https://frontend.example.com  
```

## Дальнейшее развитие

### Планируемые улучшения
1. **Real-time обновления**: WebSocket для live-обновлений очереди
2. **Кэширование**: Redis для кэширования часто запрашиваемых данных
3. **Система оповещений**: SMS/Email уведомления для клиентов
4. **Расширенная аналитика**: Dashboard с метриками производительности
5. **Мобильное приложение**: Native mobile app для клиентов

### Масштабирование
- Горизонтальное масштабирование API слоя
- Репликация PostgreSQL для отказоустойчивости
- Балансировка нагрузки через Nginx/HAProxy
- Кэширование через Redis для снижения нагрузки на БД

## Заключение

Бэкенд VQMS представляет собой современное, хорошо спроектированное приложение, построенное с использованием лучших практик разработки на .NET. Система демонстрирует хорошее разделение ответственности, поддерживаемость и расширяемость.

Ключевые преимущества:
- **Чистая архитектура**: Легко тестировать и поддерживать
- **Полноценное API**: Покрывает все сценарии использования
- **Безопасность**: Современные практики аутентификации и авторизации
- **Масштабируемость**: Готова к работе в production среде
- **Документированность**: Полная документация и Swagger UI

---
*Документация создана автоматически на основе анализа кодовой базы.*