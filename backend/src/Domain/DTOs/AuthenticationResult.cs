namespace Domain.DTOs;

/// <summary>
/// Результат успешной аутентификации
/// </summary>
public class AuthenticationResult
{
    /// <summary>
    /// ID сущности (User.Id или ClientSession.Id)
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// Тип сущности: "user" или "client"
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Логин пользователя (только для EntityType = "user")
    /// </summary>
    public string? Login { get; set; }

    /// <summary>
    /// Роли пользователя (только для EntityType = "user")
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Время истечения сессии
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// ID сессии (UserSession.Id или ClientSession.Id)
    /// </summary>
    public int SessionId { get; set; }
}