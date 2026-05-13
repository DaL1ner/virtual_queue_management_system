namespace Application.Services;

using Domain.Entities;
using Domain.Enums;
using Application.DTOs;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Сервис для управления клиентскими сессиями
/// </summary>
public class ClientSessionService
{
    private readonly AppDbContext _context;
    private readonly EventLogService _eventLogService;

    public ClientSessionService(AppDbContext context, EventLogService eventLogService)
    {
        _context = context;
        _eventLogService = eventLogService;
    }

    /// <summary>
    /// Создание или получение клиентской сессии
    /// </summary>
    public async Task<ClientSessionDto> GetOrCreateAsync(CreateClientSessionDto dto)
    {
        // Поиск активной сессии
        var session = await _context.ClientSessions
            .Where(cs => cs.DeviceFingerprint == dto.DeviceFingerprint && 
                       cs.IsActive && 
                       cs.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        if (session == null)
        {
            // Создание новой сессии
            session = new ClientSession
            {
                DeviceFingerprint = dto.DeviceFingerprint,
                IpAddress = dto.IpAddress,
                UserAgent = dto.UserAgent,
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _context.ClientSessions.Add(session);
            await _context.SaveChangesAsync();
        }
        else
        {
            // Обновление IpAddress и UserAgent
            session.IpAddress = dto.IpAddress ?? session.IpAddress;
            session.UserAgent = dto.UserAgent ?? session.UserAgent;
            session.ExpiresAt = DateTime.UtcNow.AddHours(24); // Продление
            
            await _context.SaveChangesAsync();
        }

        // Получение активного талона
        var activeTicket = await _context.Tickets
            .Where(t => t.ClientSessionId == session.Id && 
                       (t.Status == TicketStatus.Waiting || 
                        t.Status == TicketStatus.Called || 
                        t.Status == TicketStatus.Serving))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        return new ClientSessionDto(
            session.Id,
            session.DeviceFingerprint,
            session.CreatedAt,
            session.ExpiresAt,
            session.IsActive,
            activeTicket?.Id,
            activeTicket?.TicketNumber,
            activeTicket?.Status
        );
    }

    /// <summary>
    /// Аннулирование клиентской сессии
    /// </summary>
    public async Task InvalidateAsync(int sessionId, int actorUserId)
    {
        var session = await _context.ClientSessions
            .FirstOrDefaultAsync(cs => cs.Id == sessionId);

        if (session == null)
        {
            throw new NotFoundException($"ClientSession with id {sessionId} not found");
        }

        session.IsActive = false;
        await _context.SaveChangesAsync();

        // Аннулирование всех связанных активных талонов
        var activeTickets = await _context.Tickets
            .Where(t => t.ClientSessionId == sessionId && 
                       (t.Status == TicketStatus.Waiting || t.Status == TicketStatus.Called))
            .ToListAsync();

        foreach (var ticket in activeTickets)
        {
            ticket.Status = TicketStatus.Cancelled;
            ticket.CancelReason = "Client session invalidated";
            ticket.UpdatedAt = DateTime.UtcNow;
        }

        if (activeTickets.Count > 0)
        {
            await _context.SaveChangesAsync();
        }

        // Логирование
        await _eventLogService.LogAsync(
            0, // Session ID not applicable for client sessions
            null,
            actorUserId,
            EventType.ClientSessionInvalidated,
            new { sessionId, actorUserId }
        );
    }

    /// <summary>
    /// Получение сессии по ID
    /// </summary>
    public async Task<ClientSessionDto?> GetByIdAsync(int sessionId)
    {
        var session = await _context.ClientSessions
            .FirstOrDefaultAsync(cs => cs.Id == sessionId);

        if (session == null)
            return null;

        var activeTicket = await _context.Tickets
            .Where(t => t.ClientSessionId == session.Id && 
                       (t.Status == TicketStatus.Waiting || 
                        t.Status == TicketStatus.Called || 
                        t.Status == TicketStatus.Serving))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        return new ClientSessionDto(
            session.Id,
            session.DeviceFingerprint,
            session.CreatedAt,
            session.ExpiresAt,
            session.IsActive,
            activeTicket?.Id,
            activeTicket?.TicketNumber,
            activeTicket?.Status
        );
    }
}
