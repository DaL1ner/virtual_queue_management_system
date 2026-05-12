namespace Domain.Enums;

public enum EventType
{
    TicketCreated = 0,
    TicketCalled = 1,
    ServiceStarted = 2,
    ServiceServed = 3,
    ServiceSkipped = 4,
    TicketCancelled = 5,
    TicketMoved = 6,
    PriorityChanged = 7,
    PriorityEscalated = 8,
    SessionStarted = 9,
    SessionPaused = 10,
    SessionResumed = 11,
    SessionClosed = 12,
    ExecutorReady = 13,
    ExecutorNotReady = 14,
    AutoAssignment = 15,
    AutoAssignmentFailed = 16,
    QueueRenormalized = 17
}
