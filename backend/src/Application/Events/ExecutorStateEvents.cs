namespace Application.Events;

/// <summary>
/// Событие: состояние готовности исполнителя изменено
/// </summary>
public sealed class ExecutorStateChangedEvent : DomainEvent
{
    public int ExecutorStateId { get; }
    public int QueueSessionId { get; }
    public int UserId { get; }
    public bool OldIsReady { get; }
    public bool NewIsReady { get; }
    public int? ActorUserId { get; }

    public ExecutorStateChangedEvent(
        int executorStateId,
        int queueSessionId,
        int userId,
        bool oldIsReady,
        bool newIsReady,
        int? actorUserId)
    {
        ExecutorStateId = executorStateId;
        QueueSessionId = queueSessionId;
        UserId = userId;
        OldIsReady = oldIsReady;
        NewIsReady = newIsReady;
        ActorUserId = actorUserId;
    }
}