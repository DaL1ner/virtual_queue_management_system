namespace Application.Events;

/// <summary>
/// Событие: конфигурация очереди создана
/// </summary>
public sealed class QueueConfigCreatedEvent : DomainEvent
{
    public int ConfigId { get; }
    public string ConfigName { get; }
    public int CreatedById { get; }

    public QueueConfigCreatedEvent(int configId, string configName, int createdById)
    {
        ConfigId = configId;
        ConfigName = configName;
        CreatedById = createdById;
    }
}

/// <summary>
/// Событие: конфигурация очереди обновлена
/// </summary>
public sealed class QueueConfigUpdatedEvent : DomainEvent
{
    public int ConfigId { get; }
    public int ActorUserId { get; }

    public QueueConfigUpdatedEvent(int configId, int actorUserId)
    {
        ConfigId = configId;
        ActorUserId = actorUserId;
    }
}

/// <summary>
/// Событие: конфигурация очереди деактивирована
/// </summary>
public sealed class QueueConfigDeactivatedEvent : DomainEvent
{
    public int ConfigId { get; }
    public string ConfigName { get; }
    public int DeactivatedById { get; }

    public QueueConfigDeactivatedEvent(int configId, string configName, int deactivatedById)
    {
        ConfigId = configId;
        ConfigName = configName;
        DeactivatedById = deactivatedById;
    }
}
