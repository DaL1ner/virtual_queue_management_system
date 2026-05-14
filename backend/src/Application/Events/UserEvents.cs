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
