namespace Domain.Entities;

using Domain.Enums;

public class EventLog : BaseEntity
{
    public int QueueSessionId { get; set; }
    public int? TicketId { get; set; }
    public int? ActorUserId { get; set; }
    public EventType EventType { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Details { get; set; } // JSON string

    // Navigation properties
    public virtual QueueSession QueueSession { get; set; } = null!;
    public virtual Ticket? Ticket { get; set; }
    public virtual User? ActorUser { get; set; }
}
