namespace Application.Events;

using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using MediatR;
using System.Text.Json;

/// <summary>
/// Обработчик всех доменных событий для логирования в базу данных
/// </summary>
public class EventLogDomainEventHandler :
    INotificationHandler<QueueSessionCreatedEvent>,
    INotificationHandler<QueueSessionStatusChangedEvent>,
    INotificationHandler<TicketCreatedEvent>,
    INotificationHandler<TicketStatusChangedEvent>,
    INotificationHandler<TicketCancelledEvent>,
    INotificationHandler<TicketCalledEvent>,
    INotificationHandler<TicketMovedEvent>,
    INotificationHandler<PriorityChangedEvent>,
    INotificationHandler<ClientSessionCreatedEvent>,
    INotificationHandler<ClientSessionInvalidatedEvent>,
    INotificationHandler<QueueConfigCreatedEvent>,
    INotificationHandler<QueueConfigUpdatedEvent>,
    INotificationHandler<ServiceTypeCreatedEvent>,
    INotificationHandler<ServiceTypeUpdatedEvent>
{
    private readonly AppDbContext _context;

    public EventLogDomainEventHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task Handle(QueueSessionCreatedEvent notification, CancellationToken cancellationToken)
    {
        var eventLog = new EventLog
        {
            QueueSessionId = notification.SessionId,
            TicketId = null,
            ActorUserId = notification.CreatedById,
            EventType = EventType.QueueSessionCreated,
            Timestamp = notification.OccurredAt,
            Details = JsonSerializer.Serialize(new { notification.SessionId, notification.QueueConfigId, notification.CreatedById })
        };

        _context.EventLogs.Add(eventLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(QueueSessionStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        var eventLog = new EventLog
        {
            QueueSessionId = notification.SessionId,
            TicketId = null,
            ActorUserId = notification.ActorUserId,
            EventType = EventType.QueueSessionStatusChanged,
            Timestamp = notification.OccurredAt,
            Details = JsonSerializer.Serialize(new { notification.SessionId, notification.NewStatus, notification.OldStatus })
        };

        _context.EventLogs.Add(eventLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(TicketCreatedEvent notification, CancellationToken cancellationToken)
    {
        var eventLog = new EventLog
        {
            QueueSessionId = notification.QueueSessionId,
            TicketId = notification.TicketId,
            ActorUserId = null,
            EventType = EventType.TicketCreated,
            Timestamp = notification.OccurredAt,
            Details = JsonSerializer.Serialize(new { notification.TicketId, notification.QueueSessionId, notification.ClientSessionId })
        };

        _context.EventLogs.Add(eventLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(TicketStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        var eventLog = new EventLog
        {
            QueueSessionId = notification.QueueSessionId,
            TicketId = notification.TicketId,
            ActorUserId = notification.ActorUserId,
            EventType = EventType.TicketStatusChanged,
            Timestamp = notification.OccurredAt,
            Details = JsonSerializer.Serialize(new { notification.TicketId, notification.QueueSessionId, notification.NewStatus, notification.OldStatus })
        };

        _context.EventLogs.Add(eventLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(TicketCancelledEvent notification, CancellationToken cancellationToken)
    {
        var eventLog = new EventLog
        {
            QueueSessionId = notification.QueueSessionId,
            TicketId = notification.TicketId,
            ActorUserId = notification.ActorUserId,
            EventType = EventType.TicketCancelled,
            Timestamp = notification.OccurredAt,
            Details = JsonSerializer.Serialize(new { notification.TicketId, notification.QueueSessionId, notification.ActorUserId })
        };

        _context.EventLogs.Add(eventLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(TicketCalledEvent notification, CancellationToken cancellationToken)
    {
        var eventLog = new EventLog
        {
            QueueSessionId = notification.QueueSessionId,
            TicketId = notification.TicketId,
            ActorUserId = notification.ExecutorUserId,
            EventType = EventType.TicketCalled,
            Timestamp = notification.OccurredAt,
            Details = JsonSerializer.Serialize(new { notification.TicketId, notification.QueueSessionId, notification.ExecutorUserId })
        };

        _context.EventLogs.Add(eventLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(TicketMovedEvent notification, CancellationToken cancellationToken)
    {
        var eventLog = new EventLog
        {
            QueueSessionId = notification.QueueSessionId,
            TicketId = notification.TicketId,
            ActorUserId = notification.ActorUserId,
            EventType = EventType.TicketMoved,
            Timestamp = notification.OccurredAt,
            Details = JsonSerializer.Serialize(new { notification.TicketId, notification.QueueSessionId, notification.OldPosition, notification.NewPosition, notification.ActorUserId })
        };

        _context.EventLogs.Add(eventLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(PriorityChangedEvent notification, CancellationToken cancellationToken)
    {
        var eventLog = new EventLog
        {
            QueueSessionId = notification.QueueSessionId,
            TicketId = notification.TicketId,
            ActorUserId = notification.ActorUserId,
            EventType = EventType.PriorityChanged,
            Timestamp = notification.OccurredAt,
            Details = JsonSerializer.Serialize(new { notification.TicketId, notification.QueueSessionId, notification.OldPriority, notification.NewPriority, notification.ActorUserId })
        };

        _context.EventLogs.Add(eventLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(ClientSessionCreatedEvent notification, CancellationToken cancellationToken)
    {
        var eventLog = new EventLog
        {
            QueueSessionId = null,
            TicketId = null,
            ActorUserId = null,
            EventType = EventType.ClientSessionCreated,
            Timestamp = notification.OccurredAt,
            Details = JsonSerializer.Serialize(new { notification.ClientSessionId, notification.DeviceFingerprint, notification.IpAddress })
        };

        _context.EventLogs.Add(eventLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(ClientSessionInvalidatedEvent notification, CancellationToken cancellationToken)
    {
        var eventLog = new EventLog
        {
            QueueSessionId = notification.QueueSessionId,
            TicketId = null,
            ActorUserId = notification.ActorUserId,
            EventType = EventType.ClientSessionInvalidated,
            Timestamp = notification.OccurredAt,
            Details = JsonSerializer.Serialize(new { notification.ClientSessionId, notification.ActorUserId })
        };

        _context.EventLogs.Add(eventLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(QueueConfigCreatedEvent notification, CancellationToken cancellationToken)
    {
        var eventLog = new EventLog
        {
            QueueSessionId = null,
            TicketId = null,
            ActorUserId = notification.CreatedById,
            EventType = EventType.QueueConfigCreated,
            Timestamp = notification.OccurredAt,
            Details = JsonSerializer.Serialize(new { notification.ConfigId, notification.ConfigName, notification.CreatedById })
        };

        _context.EventLogs.Add(eventLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(QueueConfigUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var eventLog = new EventLog
        {
            QueueSessionId = null,
            TicketId = null,
            ActorUserId = notification.ActorUserId,
            EventType = EventType.QueueConfigUpdated,
            Timestamp = notification.OccurredAt,
            Details = JsonSerializer.Serialize(new { notification.ConfigId, notification.ActorUserId })
        };

        _context.EventLogs.Add(eventLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(ServiceTypeCreatedEvent notification, CancellationToken cancellationToken)
    {
        var eventLog = new EventLog
        {
            QueueSessionId = null,
            TicketId = null,
            ActorUserId = notification.CreatedById,
            EventType = EventType.ServiceTypeCreated,
            Timestamp = notification.OccurredAt,
            Details = JsonSerializer.Serialize(new { notification.ServiceTypeId, notification.QueueConfigId, notification.Name, notification.CreatedById })
        };

        _context.EventLogs.Add(eventLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(ServiceTypeUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var eventLog = new EventLog
        {
            QueueSessionId = null,
            TicketId = null,
            ActorUserId = notification.ActorUserId,
            EventType = EventType.ServiceTypeUpdated,
            Timestamp = notification.OccurredAt,
            Details = JsonSerializer.Serialize(new { notification.ServiceTypeId, notification.QueueConfigId, notification.ActorUserId })
        };

        _context.EventLogs.Add(eventLog);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
