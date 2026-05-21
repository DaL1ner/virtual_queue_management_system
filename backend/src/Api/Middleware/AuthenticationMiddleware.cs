using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Domain.Interfaces;
using Domain.DTOs;

namespace Api.Middleware;

/// <summary>
/// Middleware для аутентификации по Bearer token
/// Проверяет токен в заголовке Authorization, находит соответствующую сессию
/// (UserSession или ClientSession) и устанавливает ClaimsPrincipal в HttpContext
/// </summary>
public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITokenValidationService tokenValidationService)
    {
        try
        {
            // Извлечение токена из заголовка Authorization
            var token = ExtractTokenFromHeader(context.Request.Headers);
            
            if (string.IsNullOrEmpty(token))
            {
                // Токен отсутствует - продолжаем как анонимный запрос
                await _next(context);
                return;
            }

            // Валидация токена
            var authResult = await tokenValidationService.ValidateTokenAsync(token);
            
            if (authResult == null)
            {
                // Токен невалиден
                _logger.LogWarning("Invalid token provided");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized: Invalid or expired token");
                return;
            }

            // Создание ClaimsPrincipal
            var principal = CreateClaimsPrincipal(authResult);
            context.User = principal;

            // Добавление информации в Items для удобства доступа в эндпоинтах
            context.Items["AuthEntityId"] = authResult.EntityId;
            context.Items["AuthEntityType"] = authResult.EntityType;
            context.Items["AuthRoles"] = authResult.Roles;

            _logger.LogDebug("Authenticated entity: {EntityType} {EntityId}", authResult.EntityType, authResult.EntityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication middleware error");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Internal server error during authentication");
            return;
        }

        await _next(context);
    }

    private static string? ExtractTokenFromHeader(IHeaderDictionary headers)
    {
        if (!headers.TryGetValue("Authorization", out var authHeader))
            return null;

        var authHeaderValue = authHeader.ToString();
        if (string.IsNullOrEmpty(authHeaderValue))
            return null;

        // Ожидаем формат "Bearer <token>"
        const string bearerPrefix = "Bearer ";
        if (!authHeaderValue.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        return authHeaderValue.Substring(bearerPrefix.Length).Trim();
    }

    private static ClaimsPrincipal CreateClaimsPrincipal(AuthenticationResult authResult)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, authResult.EntityId.ToString()),
            new Claim("entity_type", authResult.EntityType)
        };

        // Для пользователей добавляем логин и роли
        if (authResult.EntityType == "user" && !string.IsNullOrEmpty(authResult.Login))
        {
            claims.Add(new Claim(ClaimTypes.Name, authResult.Login));
            
            if (authResult.Roles != null)
            {
                foreach (var role in authResult.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }
        }
        else if (authResult.EntityType == "client")
        {
            claims.Add(new Claim(ClaimTypes.Name, $"client_{authResult.EntityId}"));
        }

        var identity = new ClaimsIdentity(claims, "Bearer");
        return new ClaimsPrincipal(identity);
    }
}