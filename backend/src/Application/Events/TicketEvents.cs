namespace Application.Events;

using Domain.Enums;
/// <summary>
/// Событие: талон создан
/// </summary>
public sealed class TicketCreatedEvent : DomainEvent
{
    public int TicketId { get; }
    public int QueueSessionId { get; }
    public int ClientSessionId { get; }

    public TicketCreatedEvent(int ticketId, int queueSessionId, int clientSessionId)
    {
        TicketId = ticketId;
        QueueSessionId = queueSessionId;
        ClientSessionId = clientSessionId;
    }
}

/// <summary>
/// Событие: статус талона изменён
/// </summary>
public sealed class TicketStatusChangedEvent : DomainEvent
{
    public int TicketId { get; }
    public int QueueSessionId { get; }
    public TicketStatus NewStatus { get; }
    public TicketStatus? OldStatus { get; }
    public int? ActorUserId { get; }

    public TicketStatusChangedEvent(int ticketId, int queueSessionId, TicketStatus newStatus, TicketStatus? oldStatus, int? actorUserId)
    {
        TicketId = ticketId;
        QueueSessionId = queueSessionId;
        NewStatus = newStatus;
        OldStatus = oldStatus;
        ActorUserId = actorUserId;
    }
}

/// <summary>
/// Событие: талон отменён
/// </summary>
public sealed class TicketCancelledEvent : DomainEvent
{
    public int TicketId { get; }
    public int QueueSessionId { get; }
    public int? ActorUserId { get; }

    public TicketCancelledEvent(int ticketId, int queueSessionId, int? actorUserId)
    {
        TicketId = ticketId;
        QueueSessionId = queueSessionId;
        ActorUserId = actorUserId;
    }
}

/// <summary>
/// Событие: талон вызван к обслуживанию
/// </summary>
public sealed class TicketCalledEvent : DomainEvent
{
    public int TicketId { get; }
    public int QueueSessionId { get; }
    public int? ExecutorUserId { get; }

    public TicketCalledEvent(int ticketId, int queueSessionId, int? executorUserId)
    {
        TicketId = ticketId;
        QueueSessionId = queueSessionId;
        ExecutorUserId = executorUserId;
    }
}

/// <summary>
/// Событие: талон перемещён в очереди
/// </summary>
public sealed class TicketMovedEvent : DomainEvent
{
    public int TicketId { get; }
    public int QueueSessionId { get; }
    public int OldPosition { get; }
    public int NewPosition { get; }
    public int? ActorUserId { get; }

    public TicketMovedEvent(int ticketId, int queueSessionId, int oldPosition, int newPosition, int? actorUserId)
    {
        TicketId = ticketId;
        QueueSessionId = queueSessionId;
        OldPosition = oldPosition;
        NewPosition = newPosition;
        ActorUserId = actorUserId;
    }
}

/// <summary>
/// Событие: приоритет изменён
/// </summary>
public sealed class PriorityChangedEvent : DomainEvent
{
    public int TicketId { get; }
    public int QueueSessionId { get; }
    public int OldPriority { get; }
    public int NewPriority { get; }
    public int? ActorUserId { get; }

    public PriorityChangedEvent(int ticketId, int queueSessionId, int oldPriority, int newPriority, int? actorUserId)
    {
        TicketId = ticketId;
        QueueSessionId = queueSessionId;
        OldPriority = oldPriority;
        NewPriority = newPriority;
        ActorUserId = actorUserId;
    }
}
