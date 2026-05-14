namespace Application.Events;

using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using MediatR;
using System.Text.Json;

/// <summary>
/// Обработчик событий пользователей для логирования в базу данных
/// </summary>
public class UserLogDomainEventHandler :
    INotificationHandler<UserCreatedEvent>,
    INotificationHandler<UserUpdatedEvent>,
    INotificationHandler<UserRoleAssignedEvent>,
    INotificationHandler<UserRoleUnassignedEvent>
{
    private readonly AppDbContext _context;

    public UserLogDomainEventHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        var eventLog = new EventLog
        {
            QueueSessionId = null,
            TicketId = null,
            ActorUserId = notification.CreatedById,
            EventType = EventType.UserCreated,
            Timestamp = notification.OccurredAt,
            Details = JsonSerializer.Serialize(new { notification.UserId, notification.Login })
        };

        _context.EventLogs.Add(eventLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(UserUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var eventLog = new EventLog
        {
            QueueSessionId = null,
            TicketId = null,
            ActorUserId = notification.UpdatedById,
            EventType = EventType.UserUpdated,
            Timestamp = notification.OccurredAt,
            Details = JsonSerializer.Serialize(new { notification.UserId, notification.Login })
        };

        _context.EventLogs.Add(eventLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(UserRoleAssignedEvent notification, CancellationToken cancellationToken)
    {
        var eventLog = new EventLog
        {
            QueueSessionId = null,
            TicketId = null,
            ActorUserId = notification.AssignedById,
            EventType = EventType.UserRoleAssigned,
            Timestamp = notification.OccurredAt,
            Details = JsonSerializer.Serialize(new { notification.UserId, notification.RoleId, notification.RoleName })
        };

        _context.EventLogs.Add(eventLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(UserRoleUnassignedEvent notification, CancellationToken cancellationToken)
    {
        var eventLog = new EventLog
        {
            QueueSessionId = null,
            TicketId = null,
            ActorUserId = notification.UnassignedById,
            EventType = EventType.UserRoleUnassigned,
            Timestamp = notification.OccurredAt,
            Details = JsonSerializer.Serialize(new { notification.UserId, notification.RoleId, notification.RoleName })
        };

        _context.EventLogs.Add(eventLog);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
