namespace Domain.Enums;

public enum EventType
{
    // Queue Config
    QueueConfigCreated = 0,
    QueueConfigUpdated = 1,
    
    // Queue Session
    QueueSessionCreated = 2,
    QueueSessionStatusChanged = 3,
    SessionStarted = 4,      // OPEN
    SessionPaused = 5,       // PAUSED
    SessionResumed = 6,      // OPEN из PAUSED
    SessionClosed = 7,       // CLOSED
    
    // Ticket
    TicketCreated = 8,
    TicketStatusChanged = 9,
    TicketCancelled = 10,
    TicketMoved = 11,
    TicketCalled = 12,
    PriorityChanged = 13,
    PriorityEscalated = 14,  // Автоматическое повышение приоритета (не MVP)
    QueueRenormalized = 15,
    
    // Executor
    ExecutorReady = 16,
    ExecutorNotReady = 17,
    
    // Auto Assignment (не MVP)
    AutoAssignment = 18,
    AutoAssignmentFailed = 19,
    
    // Service
    ServiceStarted = 20,     // SERVING
    ServiceServed = 21,      // SERVED
    ServiceSkipped = 22,     // SKIPPED
    
    // Client Session
    ClientSessionInvalidated = 23,
    
    // Service Type
    ServiceTypeCreated = 24,
    ServiceTypeUpdated = 25,

    // User
    UserCreated = 26,
    UserUpdated = 27,
    UserRoleAssigned = 28,
    UserRoleUnassigned = 29
}
