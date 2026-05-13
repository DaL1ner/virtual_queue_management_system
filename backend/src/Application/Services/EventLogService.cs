namespace Application.Services;

using Domain.Entities;
using Domain.Enums;
using Application.DTOs;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Сервис для логирования событий
/// </summary>
public class EventLogService
{
    private readonly AppDbContext _context;

    public EventLogService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Асинхронная запись события
    /// </summary>
    public async Task LogAsync(
        int sessionId,
        int? ticketId,
        int? actorUserId,
        EventType eventType,
        object? details = null)
    {
        var eventLog = new EventLog
        {
            QueueSessionId = sessionId,
            TicketId = ticketId,
            ActorUserId = actorUserId,
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            Details = details != null ? System.Text.Json.JsonSerializer.Serialize(details) : null
        };

        _context.EventLogs.Add(eventLog);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Возвращает события по талону
    /// </summary>
    public async Task<IEnumerable<EventLogDto>> GetByTicketAsync(int ticketId)
    {
        var logs = await _context.EventLogs
            .Include(e => e.ActorUser)
            .Where(e => e.TicketId == ticketId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        return logs.Select(MapToDto);
    }

    /// <summary>
    /// Возвращает события по сессии с фильтрацией по времени
    /// </summary>
    public async Task<IEnumerable<EventLogDto>> GetBySessionAsync(
        int sessionId,
        DateTime? from = null,
        DateTime? to = null)
    {
        var query = _context.EventLogs
            .Include(e => e.ActorUser)
            .Where(e => e.QueueSessionId == sessionId);

        if (from.HasValue)
        {
            query = query.Where(e => e.Timestamp >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.Timestamp <= to.Value);
        }

        var logs = await query
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        return logs.Select(MapToDto);
    }

    /// <summary>
    /// Преобразование в EventLogDto
    /// </summary>
    private EventLogDto MapToDto(EventLog log)
    {
        return new EventLogDto(
            log.Id,
            log.QueueSessionId,
            log.TicketId,
            log.ActorUserId,
            log.ActorUser?.FullName,
            log.EventType,
            log.Timestamp,
            log.Details
        );
    }
}
