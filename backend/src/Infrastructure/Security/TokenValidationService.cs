using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Domain.Interfaces;
using Infrastructure.Data;
using Domain.DTOs;

namespace Infrastructure.Security;

public class TokenValidationService : ITokenValidationService
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly ILogger<TokenValidationService> _logger;

    public TokenValidationService(
        AppDbContext context,
        ITokenService tokenService,
        ILogger<TokenValidationService> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthenticationResult?> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        var tokenHash = _tokenService.HashToken(token);

        // 1. Попробовать найти UserSession
        var userSession = await _context.UserSessions
            .Include(us => us.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(us =>
                us.TokenHash == tokenHash &&
                us.IsActive &&
                us.ExpiresAt > DateTime.UtcNow);

        if (userSession != null)
        {
            _logger.LogDebug("Valid user session found: sessionId={SessionId}, userId={UserId}",
                userSession.Id, userSession.UserId);

            var roles = userSession.User.UserRoles
                .Select(ur => ur.Role.Code)
                .ToList();

            return new AuthenticationResult
            {
                EntityId = userSession.UserId,
                EntityType = "user",
                Login = userSession.User.Login,
                Roles = roles,
                ExpiresAt = userSession.ExpiresAt,
                SessionId = userSession.Id
            };
        }

        // 2. Попробовать найти ClientSession
        var clientSession = await _context.ClientSessions
            .FirstOrDefaultAsync(cs =>
                cs.TokenHash == tokenHash &&
                cs.IsActive &&
                cs.ExpiresAt > DateTime.UtcNow);

        if (clientSession != null)
        {
            _logger.LogDebug("Valid client session found: sessionId={SessionId}",
                clientSession.Id);

            return new AuthenticationResult
            {
                EntityId = clientSession.Id,
                EntityType = "client",
                Login = null,
                Roles = new System.Collections.Generic.List<string>(),
                ExpiresAt = clientSession.ExpiresAt,
                SessionId = clientSession.Id
            };
        }

        _logger.LogWarning("No valid session found for token hash");
        return null;
    }
}