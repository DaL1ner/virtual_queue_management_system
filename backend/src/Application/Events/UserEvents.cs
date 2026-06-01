namespace Application.Events;

/// <summary>
/// Событие: учётная запись сотрудника создана
/// </summary>
public sealed class UserCreatedEvent : DomainEvent
{
    public int UserId { get; }
    public string Login { get; }
    public int CreatedById { get; }

    public UserCreatedEvent(int userId, string login, int createdById)
    {
        UserId = userId;
        Login = login;
        CreatedById = createdById;
    }
}

/// <summary>
/// Событие: учётная запись сотрудника обновлена
/// </summary>
public sealed class UserUpdatedEvent : DomainEvent
{
    public int UserId { get; }
    public string Login { get; }
    public int UpdatedById { get; }

    public UserUpdatedEvent(int userId, string login, int updatedById)
    {
        UserId = userId;
        Login = login;
        UpdatedById = updatedById;
    }
}

/// <summary>
/// Событие: роль назначена на учётную запись
/// </summary>
public sealed class UserRoleAssignedEvent : DomainEvent
{
    public int UserId { get; }
    public int RoleId { get; }
    public string RoleName { get; }
    public int AssignedById { get; }

    public UserRoleAssignedEvent(int userId, int roleId, string roleName, int assignedById)
    {
        UserId = userId;
        RoleId = roleId;
        RoleName = roleName;
        AssignedById = assignedById;
    }
}

/// <summary>
/// Событие: роль снята с учётной записи
/// </summary>
public sealed class UserRoleUnassignedEvent : DomainEvent
{
    public int UserId { get; }
    public int RoleId { get; }
    public string RoleName { get; }
    public int UnassignedById { get; }

    public UserRoleUnassignedEvent(int userId, int roleId, string roleName, int unassignedById)
    {
        UserId = userId;
        RoleId = roleId;
        RoleName = roleName;
        UnassignedById = unassignedById;
    }
}

/// <summary>
/// Событие: сессия пользователя аннулирована (при новом логине)
/// </summary>
public sealed class UserSessionRevokedEvent : DomainEvent
{
    public int UserSessionId { get; }
    public int UserId { get; }
    public string Login { get; }

    public UserSessionRevokedEvent(int userSessionId, int userId, string login)
    {
        UserSessionId = userSessionId;
        UserId = userId;
        Login = login;
    }
}

/// <summary>
/// Событие: сессия пользователя создана (успешная аутентификация)
/// </summary>
public sealed class UserSessionCreatedEvent : DomainEvent
{
    public int UserSessionId { get; }
    public int UserId { get; }
    public string Login { get; }
    public string? IpAddress { get; }
    public string? UserAgent { get; }

    public UserSessionCreatedEvent(int userSessionId, int userId, string login, string? ipAddress, string? userAgent)
    {
        UserSessionId = userSessionId;
        UserId = userId;
        Login = login;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}

/// <summary>
/// Событие: учётная запись сотрудника активирована
/// </summary>
public sealed class UserActivatedEvent : DomainEvent
{
    public int UserId { get; }
    public string Login { get; }
    public int ActivatedById { get; }

    public UserActivatedEvent(int userId, string login, int activatedById)
    {
        UserId = userId;
        Login = login;
        ActivatedById = activatedById;
    }
}

/// <summary>
/// Событие: учётная запись сотрудника деактивирована
/// </summary>
public sealed class UserDeactivatedEvent : DomainEvent
{
    public int UserId { get; }
    public string Login { get; }
    public int DeactivatedById { get; }

    public UserDeactivatedEvent(int userId, string login, int deactivatedById)
    {
        UserId = userId;
        Login = login;
        DeactivatedById = deactivatedById;
    }
}
