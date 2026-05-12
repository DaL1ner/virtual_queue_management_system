namespace Domain.Entities;

public class ServiceType : BaseEntity
{
    public int QueueConfigId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public char Letter { get; set; }
    public int BasePriorityLevel { get; set; } = 0;
    public int? PlanAvgServiceTimeSec { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsHighlighting { get; set; } = false;
    public int SortOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual QueueConfig QueueConfig { get; set; } = null!;
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
