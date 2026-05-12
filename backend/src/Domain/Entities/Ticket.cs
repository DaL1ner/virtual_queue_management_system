namespace Domain.Entities;

using Domain.Enums;

public class Ticket : BaseEntity
{
    public int QueueSessionId { get; set; }
    public int? ServiceTypeId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientSurname { get; set; } = string.Empty;
    public decimal SortOrder { get; set; }
    public int PriorityLevel { get; set; } = 0;
    public TicketStatus Status { get; set; } = TicketStatus.Waiting;
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CalledAt { get; set; }
    public DateTime? ServiceStartedAt { get; set; }
    public DateTime? ServiceEndedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? ServedByUserId { get; set; }
    public int? ClientSessionId { get; set; }
    public string? CancelReason { get; set; }

    // Navigation properties
    public virtual QueueSession QueueSession { get; set; } = null!;
    public virtual ServiceType? ServiceType { get; set; }
    public virtual User? ServedByUser { get; set; }
    public virtual ClientSession? ClientSession { get; set; }
    public virtual ICollection<EventLog> EventLogs { get; set; } = new List<EventLog>();
}
