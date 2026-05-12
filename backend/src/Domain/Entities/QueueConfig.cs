namespace Domain.Entities;

using Domain.Enums;

public class QueueConfig : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DistributionMode DistributionMode { get; set; } = DistributionMode.Manual;
    public bool IsServiceTypeEnabled { get; set; } = false;
    public bool IsPriorityEnabled { get; set; } = true;
    public int? PriorityEscalationWaitMin { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedById { get; set; }

    // Navigation properties
    public virtual User CreatedBy { get; set; } = null!;
    public virtual ICollection<QueueSession> QueueSessions { get; set; } = new List<QueueSession>();
    public virtual ICollection<ServiceType> ServiceTypes { get; set; } = new List<ServiceType>();
}
