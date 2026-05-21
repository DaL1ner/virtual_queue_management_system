namespace Application.Services;

using Domain.Entities;
using Domain.Interfaces;
using Application.DTOs;
using Application.Events;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Сервис для управления сессиями пользователей (сотрудников)
/// </summary>
public class UserSessionService
{
    private readonly AppDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly UserService _userService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<UserSessionService> _logger;

    public UserSessionService(
        AppDbContext context,
        IEventPublisher eventPublisher,
        UserService userService,
        ITokenService tokenService,
        ILogger<UserSessionService> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _userService = userService;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Аутентификация пользователя и создание сессии
    /// </summary>
    public async Task<UserSessionResponseDto> AuthenticateAsync(LoginUserDto dto, string? ipAddress, string? userAgent)
    {
        _logger.LogInformation("Authenticating user with login '{Login}'", dto.Login);

        // 1. Аутентификация пользователя через UserService
        var user = await _userService.AuthenticateAsync(dto.Login, dto.Password);

        // 2. Завершение всех активных сессий пользователя
        var activeSessions = await GetActiveSessionsByUserIdAsync(user.Id);
        if (activeSessions.Any())
        {
            foreach (var revokedSession in activeSessions)
            {
                revokedSession.IsActive = false;
            }
            await _context.SaveChangesAsync();

            foreach (var revokedSession in activeSessions)
            {
                await _eventPublisher.PublishAsync(new UserSessionRevokedEvent(
                    revokedSession.Id,
                    user.Id,
                    user.Login));
            }

            _logger.LogInformation("All active sessions revoked for userId={UserId}", user.Id);
        }

        // 3. Генерация токена сессии
        var (token, tokenHash) = _tokenService.GenerateSessionToken();

        // 3. Создание записи сессии
        var session = new UserSession
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24), // Срок жизни 24 часа
            LastActivityAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User session created: sessionId={SessionId}, userId={UserId}", session.Id, user.Id);

        // 4. Публикация события
        await _eventPublisher.PublishAsync(new UserSessionCreatedEvent(
            session.Id,
            user.Id,
            user.Login,
            ipAddress,
            userAgent));

        // 5. Получение ролей пользователя
        var roleCodes = user.UserRoles
            .Select(ur => ur.Role.Code)
            .ToList();

        // 6. Формирование ответа
        return new UserSessionResponseDto(
            SessionId: session.Id,
            UserId: user.Id,
            Login: user.Login,
            FullName: user.FullName,
            LastName: user.LastName,
            Email: user.Email,
            Token: token,
            TokenExpiresAt: session.ExpiresAt,
            RoleCodes: roleCodes
        );
    }

    /// <summary>
    /// Получение сессии по ID
    /// </summary>
    public async Task<UserSession?> GetSessionAsync(int sessionId)
    {
        return await _context.UserSessions
            .Include(us => us.User)
            .FirstOrDefaultAsync(us => us.Id == sessionId);
    }

    /// <summary>
    /// Аннулирование сессии
    /// </summary>
    public async Task InvalidateSessionAsync(int sessionId, int actorUserId)
    {
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(us => us.Id == sessionId);

        if (session == null)
        {
            throw new NotFoundException($"UserSession with id {sessionId} not found");
        }

        session.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User session invalidated: sessionId={SessionId}, actorUserId={ActorUserId}", sessionId, actorUserId);
    }

    /// <summary>
    /// Продление сессии (обновление времени активности и срока действия)
    /// </summary>
    public async Task RefreshSessionAsync(int sessionId, string? ipAddress, string? userAgent)
    {
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(us => us.Id == sessionId && us.IsActive);

        if (session == null)
        {
            throw new NotFoundException($"Active UserSession with id {sessionId} not found");
        }

        session.LastActivityAt = DateTime.UtcNow;
        session.ExpiresAt = DateTime.UtcNow.AddHours(24); // Продление на 24 часа
        if (ipAddress != null) session.IpAddress = ipAddress;
        if (userAgent != null) session.UserAgent = userAgent;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User session refreshed: sessionId={SessionId}", sessionId);
    }

    /// <summary>
    /// Получение активных сессий пользователя
    /// </summary>
    public async Task<IEnumerable<UserSession>> GetActiveSessionsByUserIdAsync(int userId)
    {
        return await _context.UserSessions
            .Where(us => us.UserId == userId && us.IsActive && us.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(us => us.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Проверка валидности токена сессии
    /// </summary>
    public async Task<UserSession?> ValidateTokenAsync(string token)
    {
        var tokenHash = _tokenService.HashToken(token);
        var session = await _context.UserSessions
            .Include(us => us.User)
            .FirstOrDefaultAsync(us => us.TokenHash == tokenHash && us.IsActive && us.ExpiresAt > DateTime.UtcNow);

        if (session != null)
        {
            session.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return session;
    }
}
