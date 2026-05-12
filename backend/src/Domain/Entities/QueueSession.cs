namespace Domain.Entities;

using Domain.Enums;

public class QueueSession : BaseEntity
{
    public int QueueConfigId { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Draft;
    public DateTime? StartedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int CreatedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual QueueConfig QueueConfig { get; set; } = null!;
    public virtual User CreatedBy { get; set; } = null!;
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public virtual ICollection<ExecutorState> ExecutorStates { get; set; } = new List<ExecutorState>();
    public virtual ICollection<EventLog> EventLogs { get; set; } = new List<EventLog>();
}
