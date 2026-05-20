namespace Application.Services;

using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Application.DTOs;
using Application.Events;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Сервис для управления клиентскими сессиями
/// </summary>
public class ClientSessionService
{
    private readonly AppDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly TicketService _ticketService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<ClientSessionService> _logger;

    public ClientSessionService(
        AppDbContext context,
        IEventPublisher eventPublisher,
        TicketService ticketService,
        ITokenService tokenService,
        ILogger<ClientSessionService> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _ticketService = ticketService;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Создание или получение клиентской сессии с токеном
    /// </summary>
    public async Task<ClientSessionWithTokenDto> GetOrCreateAsync(CreateClientSessionDto dto)
    {
        // Поиск активной сессии
        var session = await _context.ClientSessions
            .Where(cs => cs.DeviceFingerprint == dto.DeviceFingerprint &&
                       cs.IsActive &&
                       cs.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        bool isNew = false;
        string? token = null;
        string tokenHash = dto.TokenHash;
        
        // Если токен не передан (пустая строка), генерируем новый
        if (string.IsNullOrWhiteSpace(tokenHash) && session == null)
        {
            (token, tokenHash) = _tokenService.GenerateSessionToken();
        }
        // Если сессия существует и передан пустой TokenHash, оставляем старый хэш
        else if (string.IsNullOrWhiteSpace(tokenHash) && session != null)
        {
            tokenHash = session.TokenHash;
        }
        
        if (session == null)
        {
            // Создание новой сессии
            session = new ClientSession
            {
                DeviceFingerprint = dto.DeviceFingerprint,
                TokenHash = tokenHash,
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
            // Обновление IpAddress, UserAgent и TokenHash (если передан непустой)
            session.IpAddress = dto.IpAddress ?? session.IpAddress;
            session.UserAgent = dto.UserAgent ?? session.UserAgent;
            if (!string.IsNullOrWhiteSpace(dto.TokenHash))
            {
                session.TokenHash = dto.TokenHash;
            }
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

        return new ClientSessionWithTokenDto(
            session.Id,
            session.DeviceFingerprint,
            token ?? string.Empty, // Для существующей сессии токен неизвестен
            session.TokenHash,
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
            session.TokenHash,
            session.CreatedAt,
            session.ExpiresAt,
            session.IsActive,
            activeTicket?.Id,
            activeTicket?.TicketNumber,
            activeTicket?.Status
        );
    }

    /// <summary>
    /// Массовая инвалидация клиентских сессий, связанных с талонами конкретной сессии очереди
    /// Вызывается при закрытии сессии очереди для аннулирования всех связанных клиентских сессий
    /// </summary>
    /// <returns>Количество аннулированных сессий</returns>
    public async Task<int> InvalidateAllByQueueSessionAsync(
        int queueSessionId,
        int actorUserId)
    {
        _logger.LogInformation("InvalidateAllByQueueSessionAsync: queueSessionId={QueueSessionId}", queueSessionId);

        // 1. Получить все уникальные ClientSessionId из талонов сессии очереди
        var clientSessionIds = await _context.Tickets
            .Where(t => t.QueueSessionId == queueSessionId)
            .Where(t => t.ClientSessionId.HasValue)
            .Select(t => t.ClientSessionId.Value)
            .Distinct()
            .ToListAsync();

        _logger.LogInformation("InvalidateAllByQueueSessionAsync: Found {Count} unique ClientSessionIds",
            clientSessionIds.Count);

        if (clientSessionIds.Count == 0)
        {
            _logger.LogWarning("InvalidateAllByQueueSessionAsync: No ClientSessionIds found for queueSessionId={QueueSessionId}", queueSessionId);
            return 0;
        }

        // 2. Получить активные сессии
        var activeSessions = await _context.ClientSessions
            .Where(cs => clientSessionIds.Contains(cs.Id) && cs.IsActive)
            .ToListAsync();

        _logger.LogInformation("InvalidateAllByQueueSessionAsync: Found {Count} active ClientSessions",
            activeSessions.Count);

        if (activeSessions.Count == 0)
        {
            _logger.LogWarning("InvalidateAllByQueueSessionAsync: No active ClientSessions found for queueSessionId={QueueSessionId}", queueSessionId);
            return 0;
        }

        // 3. Аннулировать сессии
        foreach (var session in activeSessions)
        {
            session.IsActive = false;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("InvalidateAllByQueueSessionAsync: Invalidated {Count} ClientSessions for queueSessionId={QueueSessionId}",
            activeSessions.Count, queueSessionId);

        // 4. Публикация событий для каждой аннулированной сессии
        foreach (var session in activeSessions)
        {
            await _eventPublisher.PublishAsync(
                new ClientSessionInvalidatedEvent(session.Id, null, actorUserId));
        }

        return activeSessions.Count;
    }
}
