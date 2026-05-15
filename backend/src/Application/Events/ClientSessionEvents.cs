namespace Application.Events;

/// <summary>
/// Событие: клиентская сессия создана
/// </summary>
public sealed class ClientSessionCreatedEvent : DomainEvent
{
    public int ClientSessionId { get; }
    public string DeviceFingerprint { get; }
    public string? IpAddress { get; }

    public ClientSessionCreatedEvent(int clientSessionId, string deviceFingerprint, string? ipAddress)
    {
        ClientSessionId = clientSessionId;
        DeviceFingerprint = deviceFingerprint;
        IpAddress = ipAddress;
    }
}

/// <summary>
/// Событие: клиентская сессия аннулирована
/// </summary>
public sealed class ClientSessionInvalidatedEvent : DomainEvent
{
    public int ClientSessionId { get; }
    public int? QueueSessionId { get; }
    public int ActorUserId { get; }

    public ClientSessionInvalidatedEvent(int clientSessionId, int? queueSessionId, int actorUserId)
    {
        ClientSessionId = clientSessionId;
        QueueSessionId = queueSessionId;
        ActorUserId = actorUserId;
    }
}
