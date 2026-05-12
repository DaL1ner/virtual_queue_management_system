namespace Domain.Entities;

public class ExecutorState : BaseEntity
{
    public int QueueSessionId { get; set; }
    public int UserId { get; set; }
    public bool IsReady { get; set; } = false;
    public int? CurrentTicketId { get; set; }
    public DateTime LastStatusChange { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual QueueSession QueueSession { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual Ticket? CurrentTicket { get; set; }
}
