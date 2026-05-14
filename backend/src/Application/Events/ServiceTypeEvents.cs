namespace Application.Events;

/// <summary>
/// Событие: тип услуги создан
/// </summary>
public sealed class ServiceTypeCreatedEvent : DomainEvent
{
    public int ServiceTypeId { get; }
    public int QueueConfigId { get; }
    public string Name { get; }
    public int CreatedById { get; }

    public ServiceTypeCreatedEvent(int serviceTypeId, int queueConfigId, string name, int createdById)
    {
        ServiceTypeId = serviceTypeId;
        QueueConfigId = queueConfigId;
        Name = name;
        CreatedById = createdById;
    }
}

/// <summary>
/// Событие: тип услуги обновлён
/// </summary>
public sealed class ServiceTypeUpdatedEvent : DomainEvent
{
    public int ServiceTypeId { get; }
    public int QueueConfigId { get; }
    public int ActorUserId { get; }

    public ServiceTypeUpdatedEvent(int serviceTypeId, int queueConfigId, int actorUserId)
    {
        ServiceTypeId = serviceTypeId;
        QueueConfigId = queueConfigId;
        ActorUserId = actorUserId;
    }
}
