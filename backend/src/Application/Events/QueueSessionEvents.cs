namespace Application.Events;

using Domain.Enums;

/// <summary>
/// Событие: сессия очереди создана
/// </summary>
public sealed class QueueSessionCreatedEvent : DomainEvent
{
    public int SessionId { get; }
    public int QueueConfigId { get; }
    public int CreatedById { get; }

    public QueueSessionCreatedEvent(int sessionId, int queueConfigId, int createdById)
    {
        SessionId = sessionId;
        QueueConfigId = queueConfigId;
        CreatedById = createdById;
    }
}

/// <summary>
/// Событие: статус сессии очереди изменён
/// </summary>
public sealed class QueueSessionStatusChangedEvent : DomainEvent
{
    public int SessionId { get; }
    public SessionStatus NewStatus { get; }
    public SessionStatus? OldStatus { get; }
    public int ActorUserId { get; }

    public QueueSessionStatusChangedEvent(int sessionId, SessionStatus newStatus, SessionStatus? oldStatus, int actorUserId)
    {
        SessionId = sessionId;
        NewStatus = newStatus;
        OldStatus = oldStatus;
        ActorUserId = actorUserId;
    }
}
