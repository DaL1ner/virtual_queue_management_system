namespace Application.Services;

using Domain.Entities;
using Domain.Enums;
using Application.DTOs;
using Application.Events;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Сервис для управления клиентскими сессиями
/// </summary>
public class ClientSessionService
{
    private readonly AppDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly TicketService _ticketService;

    public ClientSessionService(
        AppDbContext context, 
        IEventPublisher eventPublisher,
        TicketService ticketService)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _ticketService = ticketService;
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

        bool isNew = false;
        
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
            isNew = true;
        }
        else
        {
            // Обновление IpAddress и UserAgent
            session.IpAddress = dto.IpAddress ?? session.IpAddress;
            session.UserAgent = dto.UserAgent ?? session.UserAgent;
            session.ExpiresAt = DateTime.UtcNow.AddHours(24); // Продление
            
            await _context.SaveChangesAsync();
        }

        // Публикация события создания сессии
        if (isNew)
        {
            await _eventPublisher.PublishAsync(new ClientSessionCreatedEvent(
                session.Id, 
                session.DeviceFingerprint, 
                session.IpAddress));
        }

        // Получение активного талона через TicketService
        var activeTicket = await _ticketService.GetActiveTicketByClientSessionIdAsync(session.Id);

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

        // Аннулирование всех связанных активных талонов через TicketService
        await _ticketService.InvalidateTicketsByClientSessionIdAsync(
            sessionId, 
            "Client session invalidated", 
            actorUserId);

        // Публикация доменного события
        await _eventPublisher.PublishAsync(new ClientSessionInvalidatedEvent(
            sessionId, null, actorUserId));
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

        // Получение активного талона через TicketService
        var activeTicket = await _ticketService.GetActiveTicketByClientSessionIdAsync(session.Id);

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
