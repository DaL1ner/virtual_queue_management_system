namespace Domain.Entities;

public class ClientSession : BaseEntity
{
    public string DeviceFingerprint { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
    public bool IsActive { get; set; } = true;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Navigation properties
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
