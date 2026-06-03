# Документация бэкенда Virtual Queue Management System (VQMS) - Часть 2: Детали реализации

## Domain Layer: Детализация

### Сущности (Entities)

#### 1. User (Пользователь)
**Назначение**: Представляет пользователя системы (администратор, оператор, исполнитель).

**Поля**:  
- `Id` (int) - Уникальный идентификатор  
- `Login` (string) - Уникальный логин для входа  
- `PasswordHash` (string) - Хеш пароля  
- `FullName` (string) - Полное имя  
- `LastName` (string) - Фамилия  
- `Email` (string?) - Электронная почта (уникальная)  
- `IsActive` (bool) - Активен ли пользователь  
- `CreatedAt` (DateTime) - Дата создания  
- `UpdatedAt` (DateTime?) - Дата последнего обновления  

**Навигационные свойства**:  
- `UserRoles` - Роли пользователя  
- `UserSessions` - Сессии пользователя  
- `CreatedQueueConfigs` - Созданные конфигурации очередей  
- `CreatedQueueSessions` - Созданные сессии очередей  
- `ServedTickets` - Обслуженные талоны  
- `EventLogs` - События, связанные с пользователем  

#### 2. Ticket (Талон)
**Назначение**: Основная единица очереди - запись клиента на обслуживание.

**Поля**:  
- `Id` (int) - Уникальный идентификатор  
- `QueueSessionId` (int) - Ссылка на сессию очереди  
- `ServiceTypeId` (int?) - Тип услуги (опционально)  
- `TicketNumber` (string) - Номер талона (например, "A-001")  
- `ClientName` (string) - Имя клиента  
- `ClientSurname` (string) - Фамилия клиента  
- `SortOrder` (decimal) - Порядок сортировки в очереди  
- `PriorityLevel` (int) - Уровень приоритета (0 - нормальный)  
- `Status` (TicketStatus) - Текущий статус  
- `Version` (int) - Версия для оптимистичной блокировки  
- `CreatedAt` (DateTime) - Время создания  
- `CalledAt` (DateTime?) - Время вызова  
- `ServiceStartedAt` (DateTime?) - Время начала обслуживания  
- `ServiceEndedAt` (DateTime?) - Время окончания обслуживания  
- `UpdatedAt` (DateTime?) - Время последнего обновления  
- `ServedByUserId` (int?) - ID пользователя, обслуживающего талон  
- `ClientSessionId` (int?) - ID клиентской сессии  
- `CancelReason` (string?) - Причина отмены  

**Навигационные свойства**:  
- `QueueSession` - Сессия очереди  
- `ServiceType` - Тип услуги  
- `ServedByUser` - Пользователь-исполнитель  
- `ClientSession` - Клиентская сессия  
- `EventLogs` - События талона  

#### 3. QueueSession (Сессия очереди)
**Назначение**: Представляет рабочую сессию очереди в определенный период времени.

**Поля**:  
- `Id` (int) - Уникальный идентификатор  
- `QueueConfigId` (int) - Конфигурация очереди  
- `Name` (string) - Название сессии  
- `Description` (string?) - Описание  
- `Status` (SessionStatus) - Статус (DRAFT, OPEN, PAUSED, CLOSED)  
- `StartedAt` (DateTime?) - Время начала  
- `ClosedAt` (DateTime?) - Время закрытия  
- `CreatedByUserId` (int) - Создатель  
- `CreatedAt` (DateTime) - Время создания  
- `UpdatedAt` (DateTime?) - Время обновления  

**Навигационные свойства**:  
- `QueueConfig` - Конфигурация очереди  
- `CreatedByUser` - Пользователь-создатель  
- `Tickets` - Талоны в сессии  
- `ClientSessions` - Клиентские сессии  

#### 4. QueueConfig (Конфигурация очереди)
**Назначение**: Настройки и параметры очереди.

**Поля**:  
- `Id` (int) - Уникальный идентификатор  
- `Name` (string) - Название конфигурации  
- `Description` (string?) - Описание  
- `IsActive` (bool) - Активна ли конфигурация  
- `IsServiceTypeEnabled` (bool) - Включены ли типы услуг  
- `DistributionMode` (DistributionMode) - Режим распределения (ROUND_ROBIN, PRIORITY)  
- `AverageServiceTimeMinutes` (int) - Среднее время обслуживания (минуты)  
- `MaxWaitingTimeMinutes` (int) - Максимальное время ожидания  
- `CreatedByUserId` (int) - Создатель  
- `CreatedAt` (DateTime) - Время создания  
- `UpdatedAt` (DateTime?) - Время обновления  

**Навигационные свойства**:  
- `CreatedByUser` - Пользователь-создатель  
- `QueueSessions` - Сессии очереди  
- `ServiceTypes` - Типы услуг  

#### 5. ServiceType (Тип услуги)
**Назначение**: Категоризация услуг для талонов.

**Поля**:  
- `Id` (int) - Уникальный идентификатор  
- `QueueConfigId` (int) - Конфигурация очереди  
- `Name` (string) - Название типа  
- `Letter` (char) - Буква для номера талона (A, B, C...)  
- `Description` (string?) - Описание  
- `BasePriorityLevel` (int) - Базовый приоритет  
- `IsActive` (bool) - Активен ли тип  
- `SortOrder` (int) - Порядок сортировки  
- `CreatedAt` (DateTime) - Время создания  
- `UpdatedAt` (DateTime?) - Время обновления  

**Навигационные свойства**:  
- `QueueConfig` - Конфигурация очереди  
- `Tickets` - Талоны этого типа  

#### 6. ExecutorState (Состояние исполнителя)
**Назначение**: Отслеживание состояния оператора/исполнителя.

**Поля**:  
- `Id` (int) - Уникальный идентификатор  
- `UserId` (int) - Пользователь-исполнитель  
- `QueueSessionId` (int) - Сессия очереди  
- `IsReady` (bool) - Готов принимать талоны  
- `CurrentTicketId` (int?) - Текущий обслуживаемый талон  
- `LastStatusChange` (DateTime) - Время последнего изменения статуса  
- `CreatedAt` (DateTime) - Время создания  
- `UpdatedAt` (DateTime?) - Время обновления  

**Навигационные свойства**:  
- `User` - Пользователь-исполнитель  
- `QueueSession` - Сессия очереди  
- `CurrentTicket` - Текущий талон  

#### 7. ClientSession (Клиентская сессия)
**Назначение**: Сессия клиента для отслеживания состояния в системе.

**Поля**:  
- `Id` (int) - Уникальный идентификатор  
- `DeviceFingerprint` (string) - Уникальный идентификатор устройства  
- `TokenHash` (string) - Хеш токена сессии  
- `IpAddress` (string?) - IP-адрес  
- `UserAgent` (string?) - User-Agent браузера  
- `IsActive` (bool) - Активна ли сессия  
- `CreatedAt` (DateTime) - Время создания  
- `UpdatedAt` (DateTime?) - Время обновления  

**Навигационные свойства**:  
- `Tickets` - Талоны клиента  
- `QueueSession` - Сессия очереди  

### Перечисления (Enums)

#### TicketStatus
```csharp
public enum TicketStatus
{
    Waiting = 0,    // Ожидает вызова
    Called = 1,     // Вызван
    Serving = 2,    // Обслуживается
    Served = 3,     // Обслужен
    Skipped = 4,    // Пропущен
    Cancelled = 5   // Отменен
}
```

#### SessionStatus
```csharp
public enum SessionStatus
{
    DRAFT = 0,      // Черновик
    OPEN = 1,       // Открыта
    PAUSED = 2,     // Приостановлена
    CLOSED = 3      // Закрыта
}
```

#### DistributionMode
```csharp
public enum DistributionMode
{
    ROUND_ROBIN = 0,    // Циклическое распределение
    PRIORITY = 1        // По приоритету
}
```

#### EventType
```csharp
public enum EventType
{
    TICKET_CREATED = 0,
    TICKET_CALLED = 1,
    TICKET_SERVING_STARTED = 2,
    TICKET_SERVED = 3,
    TICKET_CANCELLED = 4,
    TICKET_SKIPPED = 5,
    QUEUE_SESSION_OPENED = 6,
    QUEUE_SESSION_CLOSED = 7,
    USER_LOGIN = 8,
    USER_LOGOUT = 9
}
```

### Интерфейсы (Interfaces)

#### ITokenService
```csharp
public interface ITokenService
{
    (string Token, string Hash) GenerateSessionToken();
    string HashToken(string token);
    bool VerifyToken(string token, string hash);
}
```

#### IPasswordHasher
```csharp
public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
```

#### ITokenValidationService
```csharp
public interface ITokenValidationService
{
    Task<AuthenticationResult?> ValidateTokenAsync(string token);
}
```

## Application Layer: Детализация

### Сервисы (Services)

#### TicketService
**Назначение**: Основной сервис для управления талонами.

**Ключевые методы**:
1. `CreateAsync()` - Создание нового талона
2. `CallAsync()` - Вызов талона оператором
3. `StartServiceAsync()` - Начало обслуживания
4. `CompleteServiceAsync()` - Завершение обслуживания
5. `CancelAsync()` - Отмена талона
6. `GetActiveTicketAsync()` - Получение активного талона клиента
7. `GetQueueAsync()` - Получение очереди талонов
8. `MoveBackwardAsync()` - Перемещение талона назад в очереди
9. `MoveToPositionAsync()` - Перемещение талона на конкретную позицию

**Логика создания талона**:
```csharp
public async Task<TicketDto> CreateAsync(CreateTicketDto dto, int? clientSessionId = null, int? actorUserId = null)
{
    // 1. Получение активной сессии очереди
    var session = await _queueSessionService.GetActiveSessionAsync();
    
    // 2. Определение типа услуги и приоритета
    ServiceType? serviceType = null;
    if (session.QueueConfig.IsServiceTypeEnabled && dto.ServiceTypeId.HasValue)
    {
        serviceType = await _context.ServiceTypes
            .FirstOrDefaultAsync(st => st.Id == dto.ServiceTypeId && st.QueueConfigId == session.QueueConfigId && st.IsActive);
    }
    
    // 3. Аннулирование предыдущих активных талонов для этой клиентской сессии
    if (clientSessionId.HasValue)
    {
        var activeTickets = await _context.Tickets
            .Where(t => t.ClientSessionId == clientSessionId &&
                       t.QueueSessionId == session.Id &&
                       (t.Status == TicketStatus.Waiting || t.Status == TicketStatus.Called))
            .ToListAsync();
        // Отмена существующих талонов
    }
    
    // 4. Генерация номера талона
    var nextNumber = await GetNextTicketNumberAsync(session.Id, serviceType?.Letter ?? 'A');
    
    // 5. Вычисление sort_order
    var maxSortOrder = await _context.Tickets
        .Where(t => t.QueueSessionId == session.Id)
        .MaxAsync(t => (decimal?)t.SortOrder) ?? 0;
    var newSortOrder = maxSortOrder + 1000;
    
    // 6. Создание и сохранение талона
    var ticket = new Ticket { ... };
    _context.Tickets.Add(ticket);
    await _context.SaveChangesAsync();
    
    // 7. Публикация события
    await _eventPublisher.PublishAsync(new TicketCreatedEvent(...));
    
    // 8. Возврат DTO
    return await MapToDtoAsync(ticket);
}
```

**Алгоритм расчета позиции в очереди**:
```csharp
private async Task<int> CalculatePositionAsync(Ticket ticket)
{
    return await _context.Tickets
        .CountAsync(t => t.QueueSessionId == ticket.QueueSessionId && t.Status == TicketStatus.Waiting && (t.PriorityLevel > ticket.PriorityLevel || (t.PriorityLevel == ticket.PriorityLevel && t.SortOrder < ticket.SortOrder) || (t.PriorityLevel == ticket.PriorityLevel && t.SortOrder == ticket.SortOrder && t.CreatedAt < ticket.CreatedAt)));
}
```

#### QueueSessionService
**Назначение**: Управление сессиями очереди.

**Ключевые методы**:
1. `GetActiveSessionAsync()` - Получение активной сессии
2. `OpenSessionAsync()` - Открытие новой сессии
3. `CloseSessionAsync()` - Закрытие сессии
4. `PauseSessionAsync()` - Приостановка сессии
5. `ResumeSessionAsync()` - Возобновление сессии

**Логика открытия сессии**:  
- Проверка, что нет других активных сессий для этой конфигурации  
- Создание записи QueueSession со статусом OPEN  
- Инициализация связанных сущностей (ExecutorState для исполнителей)  

#### UserService
**Назначение**: Управление пользователями и аутентификация.

**Ключевые методы**:
1. `AuthenticateAsync()` - Аутентификация по логину/паролю
2. `CreateAsync()` - Создание нового пользователя
3. `UpdateAsync()` - Обновление пользователя (включая пароль и синхронизацию ролей)
4. `AssignRoleAsync()` - Назначение роли пользователю
5. `UnassignRoleAsync()` - Снятие роли у пользователя
6. `DeactivateAsync()` - Деактивация учётной записи (soft delete)
7. `ActivateAsync()` - Активация учётной записи

**Обновление пользователя (`UpdateAsync`)**:
- Принимает `UpdateUserDto` с опциональными полями: `FullName`, `LastName`, `Email`, `Password`, `IsActive`, `RoleIds`
- Если передан `Password` — хеширует и сохраняет новый пароль
- Если передан `RoleIds` — синхронизирует роли: удаляет снятые, добавляет новые
- Публикует событие `UserUpdatedEvent`

#### RoleService
**Назначение**: Управление ролями.

**Ключевые методы**:
1. `GetAllAsync()` - Получение списка всех ролей

**Описание**: Сервис предоставляет доступ к справочнику ролей для использования в интерфейсе администратора (выбор ролей при создании/редактировании пользователя).

**Логика аутентификации**:
```csharp
public async Task<AuthenticationResult> AuthenticateAsync(string login, string password)
{
    var user = await _context.Users
        .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
        .FirstOrDefaultAsync(u => u.Login == login && u.IsActive);
    
    if (user == null || !_passwordHasher.VerifyPassword(password, user.PasswordHash))
        throw new UnauthorizedException("Invalid credentials");
    
    // Генерация токена сессии
    var (token, hash) = _tokenService.GenerateSessionToken();
    
    // Создание UserSession
    var userSession = new UserSession
    {
        UserId = user.Id,
        TokenHash = hash,
        IpAddress = ipAddress,
        UserAgent = userAgent,
        IsActive = true
    };
    
    _context.UserSessions.Add(userSession);
    await _context.SaveChangesAsync();
    
    return new AuthenticationResult
    {
        Token = token,
        UserId = user.Id,
        Login = user.Login,
        Roles = user.UserRoles.Select(ur => ur.Role.Code).ToList()
    };
}
```

### Data Transfer Objects (DTOs)

#### Структура DTO
Каждый DTO представляет собой `record` для иммутабельности и удобства использования с Minimal API.

**Примеры**:
```csharp
// DTO для создания талона
public record CreateTicketDto(
    string ClientName,
    string ClientSurname,
    int? ServiceTypeId = null
);

// DTO ответа талона
public record TicketDto(
    int Id,
    int QueueSessionId,
    string TicketNumber,
    string ClientName,
    string ClientSurname,
    int? ServiceTypeId,
    string? ServiceTypeName,
    char? ServiceLetter,
    int SortOrder,
    int PriorityLevel,
    TicketStatus Status,
    int Version,
    DateTime CreatedAt,
    DateTime? CalledAt,
    DateTime? ServiceStartedAt,
    DateTime? ServiceEndedAt,
    int? ServedByUserId,
    string? ServedByUserName,
    string? CancelReason,
    int PositionInQueue
);

// DTO для списка талонов
public record TicketListDto(
    IEnumerable<TicketDto> Tickets,
    int TotalCount,
    int YourPosition
);
```

### События (Events) и MediatR

#### Архитектура событий
Система использует паттерн Domain Events с MediatR для обработки событий.

**Базовое событие**:
```csharp
public abstract class DomainEvent : INotification
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
```

**Пример события**:
```csharp
public class TicketCreatedEvent : DomainEvent
{
    public int TicketId { get; }
    public int QueueSessionId { get; }
    public int ClientSessionId { get; }
    
    public TicketCreatedEvent(int ticketId, int queueSessionId, int clientSessionId)
    {
        TicketId = ticketId;
        QueueSessionId = queueSessionId;
        ClientSessionId = clientSessionId;
    }
}
```

**Обработчик событий**:
```csharp
public class EventLogDomainEventHandler : INotificationHandler<DomainEvent>
{
    private readonly AppDbContext _context;
    
    public async Task Handle(DomainEvent notification, CancellationToken cancellationToken)
    {
        var eventLog = new EventLog
        {
            EventType = notification.GetType().Name,
            EntityType = notification.GetType().GetGenericArguments().FirstOrDefault()?.Name ?? "Unknown",
            EntityId = ExtractEntityId(notification),
            Payload = JsonSerializer.Serialize(notification),
            Timestamp = notification.OccurredAt
        };
        
        _context.EventLogs.Add(eventLog);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

**Публикатор событий**:
```csharp
public class MediatREventPublisher : IEventPublisher
{
    private readonly IMediator _mediator;
    
    public async Task PublishAsync<TDomainEvent>(TDomainEvent domainEvent) 
        where TDomainEvent : class, INotification
    {
        await _mediator.Publish(domainEvent);
    }
}
```

## Infrastructure Layer: Детализация

### AppDbContext

#### Конфигурация сущностей
Каждая сущность конфигурируется через Fluent API в `OnModelCreating`.

**Пример конфигурации Ticket**:
```csharp
modelBuilder.Entity<Ticket>(entity =>
{
    entity.ToTable("tickets");
    entity.HasKey(t => t.Id);
    
    entity.Property(t => t.Id).HasColumnName("id");
    entity.Property(t => t.TicketNumber)
        .HasColumnName("ticket_number")
        .IsRequired()
        .HasMaxLength(20);
    
    // Индексы для оптимизации
    entity.HasIndex(t => new { t.QueueSessionId, t.Status })
        .HasName("idx_ticket_session_status");
    entity.HasIndex(t => new { t.ClientSessionId, t.Status })
        .HasName("idx_ticket_client_session");
    entity.HasIndex(t => new { t.QueueSessionId, t.ServiceTypeId, t.Status })
        .HasName("idx_ticket_service_type");
    
    // Ограничения
    entity.HasCheckConstraint("chk_ticket_valid_status_transition",
        @"CHECK (
            (status = 'Waiting' AND called_at IS NULL AND service_started_at IS NULL AND service_ended_at IS NULL) OR
            (status = 'Called' AND called_at IS NOT NULL AND service_started_at IS NULL AND service_ended_at IS NULL) OR
            (status = 'Serving' AND called_at IS NOT NULL AND service_started_at IS NOT NULL AND service_ended_at IS NULL) OR
            (status IN ('Served', 'Skipped', 'Cancelled') AND called_at IS NOT NULL AND service_started_at IS NOT NULL AND service_ended_at IS NOT NULL)
        )");
});
```

#### Конвертеры для перечислений
```csharp
public class TicketStatusToStringConverter : ValueConverter<TicketStatus, string>
{
    public TicketStatusToStringConverter()
        : base(
            v => v.ToString(),
            v => (TicketStatus)Enum.Parse(typeof(TicketStatus), v))
    {
    }
}
```

#### Конвертеры для DateTime
```csharp
public class DateTimeUtcConverter : ValueConverter<DateTime, DateTime>
{
    public DateTimeUtcConverter()
        : base(
            v => v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}
```

### Сервисы безопасности

#### TokenService
**Реализация**: Генерация криптографически безопасных токенов.

```csharp
public class TokenService : ITokenService
{
    public (string Token, string Hash) GenerateSessionToken()
    {
        // Генерация случайного токена
        var tokenBytes = new byte[32];
        RandomNumberGenerator.Fill(tokenBytes);
        var token = Convert.ToBase64String(tokenBytes);
        
        // Хеширование для безопасного хранения
        var hash = HashToken(token);
        
        return (token, hash);
    }
    
    public string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }
    
    public bool VerifyToken(string token, string hash)
    {
        var computedHash = HashToken(token);
        return computedHash == hash;
    }
}
```

#### PasswordHasher
**Реализация**: Использование PBKDF2 с HMAC-SHA256.

```csharp
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100000;
    
    public string HashPassword(string password)
    {
        // Генерация соли
        var salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);
        
        // Хеширование пароля
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);
        
        // Формат: iterations.salt.hash
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }
    
    public bool VerifyPassword(string password, string hashedPassword)
    {
        var parts = hashedPassword.Split('.');
        if (parts.Length != 3) return false;
        
        var iterations = int.Parse(parts[0]);
        var salt = Convert.FromBase64String(parts[1]);
        var hash = Convert.FromBase64String(parts[2]);
        
        var testHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            hash.Length);
        
        return CryptographicOperations.FixedTimeEquals(hash, testHash);
    }
}
```

#### TokenValidationService
**Реализация**: Валидация токенов и поиск соответствующих сессий.

```csharp
public class TokenValidationService : ITokenValidationService
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    
    public async Task<AuthenticationResult?> ValidateTokenAsync(string token)
    {
        // Поиск UserSession по хешу токена
        var tokenHash = _tokenService.HashToken(token);
        var userSession = await _context.UserSessions
            .Include(us => us.User)
            .ThenInclude(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(us => us.TokenHash == tokenHash && us.IsActive);
        
        if (userSession != null)
        {
            // Обновление времени последней активности
            userSession.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            return new AuthenticationResult
            {
                EntityId = userSession.UserId,
                EntityType = "user",
                Login = userSession.User.Login,
                Roles = userSession.User.UserRoles.Select(ur => ur.Role.Code).ToList()
            };
        }
        
        // Поиск ClientSession
        var clientSession = await _context.ClientSessions
            .FirstOrDefaultAsync(cs => cs.TokenHash == tokenHash && cs.IsActive);
        
        if (clientSession != null)
        {
            return new AuthenticationResult
            {
                EntityId = clientSession.Id,
                EntityType = "client",
                Login = null,
                Roles = new List<string>()
            };
        }
        
        return null;
    }
}
```

### Dependency Injection

#### Регистрация сервисов
```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // База данных
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        
        // Безопасность
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenValidationService, TokenValidationService>();
        
        return services;
    }
}
```

## Паттерны и принципы проектирования

### 1. Repository Pattern
Хотя используется прямой DbContext, сервисы действуют как репозитории с четко определенными методами доступа к данным.

### 2. Unit of Work
DbContext реализует Unit of Work, обеспечивая атомарность операций.

### 3. Strategy Pattern
Различные алгоритмы распределения талонов (ROUND_ROBIN, PRIORITY) реализованы через стратегии.

### 4. Observer Pattern
Система событий использует MediatR для реализации Observer pattern.

### 5. Factory Method
Генерация номеров талонов и токенов использует фабричные методы.

### 6. Dependency Injection
Все зависимости внедряются через конструктор, что обеспечивает тестируемость.

## Обработка ошибок и исключения

### Кастомные исключения
```csharp
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}
```

### Глобальная обработка ошибок
В endpoints используется try-catch с преобразованием исключений в соответствующие HTTP статусы:  
- `NotFoundException` -> 404 Not Found  
- `BadRequestException` -> 400 Bad Request  
- `ConflictException` -> 409 Conflict  
- `UnauthorizedException` -> 401 Unauthorized  
- Остальные -> 500 Internal Server Error  

## Тестируемость

### Мокируемые зависимости
Все внешние зависимости представлены через интерфейсы:  
- `ITokenService` - Генерация и проверка токенов  
- `IPasswordHasher` - Хеширование паролей  
- `ITokenValidationService` - Валидация токенов  
- `IEventPublisher` - Публикация событий  
- `IDbContext` - Абстракция доступа к данным  

### Изоляция тестов
- **Unit тесты**: Тестирование сервисов с моками зависимостей  
- **Integration тесты**: Тестирование с реальной БД в памяти  
- **API тесты**: Тестирование endpoints через TestServer  

## Производительность и оптимизация

### Индексы базы данных
Ключевые индексы для оптимизации запросов:
1. `idx_ticket_session_status` - Быстрый поиск талонов по сессии и статусу  
2. `idx_ticket_client_session` - Поиск активных талонов клиента  
3. `idx_session_queue_status` - Поиск активных сессий  
4. `idx_user_login` - Быстрая аутентификация по логину  
5. `idx_user_sessions_token` - Быстрая валидация токенов  

### Асинхронные операции
Все операции ввода-вывода асинхронные:  
- Асинхронные методы EF Core (`ToListAsync`, `FirstOrDefaultAsync`)  
- Асинхронное сохранение (`SaveChangesAsync`)  
- Асинхронная публикация событий (`PublishAsync`)  

### Кэширование
Планируемые улучшения:  
- Кэширование активной сессии очереди  
- Кэширование очереди талонов  
- Кэширование состояний исполнителей  

## Заключение

Реализация бэкенда VQMS демонстрирует применение современных практик разработки на .NET. Система построена с учетом принципов SOLID, Clean Architecture и Domain-Driven Design. Детальная проработка каждого слоя обеспечивает поддерживаемость, тестируемость и расширяемость кода.  
  
В следующей части документации будут рассмотрены API endpoints, их спецификации и интеграционные аспекты системы.