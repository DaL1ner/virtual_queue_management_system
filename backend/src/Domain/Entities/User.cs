namespace Domain.Entities;

public class User : BaseEntity
{
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
    public virtual ICollection<QueueConfig> CreatedQueueConfigs { get; set; } = new List<QueueConfig>();
    public virtual ICollection<QueueSession> CreatedQueueSessions { get; set; } = new List<QueueSession>();
    public virtual ICollection<Ticket> ServedTickets { get; set; } = new List<Ticket>();
    public virtual ICollection<EventLog> EventLogs { get; set; } = new List<EventLog>();
}
