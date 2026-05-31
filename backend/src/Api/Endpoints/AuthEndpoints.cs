namespace Api.Endpoints;

using Application.DTOs;
using Application.Services;
using Domain.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointGroup = app.MapGroup("/api/auth").WithTags("Auth");

        endpointGroup.MapPost("/login", Login);
        endpointGroup.MapPost("/logout", Logout);

        return endpointGroup;
    }

    private static async Task<IResult> Login(
        LoginUserDto dto,
        UserSessionService sessionService,
        HttpContext httpContext)
    {
        // Извлечение IP-адреса и User-Agent
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();

        try
        {
            var response = await sessionService.AuthenticateAsync(dto, ipAddress, userAgent);
            return Results.Ok(response);
        }
        catch (NotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedException ex)
        {
            return Results.Unauthorized();
        }
        catch (BadRequestException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            // Логирование внутренней ошибки
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> Logout(
        HttpContext httpContext,
        UserSessionService sessionService,
        ITokenValidationService tokenValidationService)
    {
        // Извлечение токена из заголовка Authorization
        var token = ExtractTokenFromHeader(httpContext.Request.Headers);

        if (string.IsNullOrEmpty(token))
        {
            return Results.BadRequest(new { error = "Token is missing" });
        }

        try
        {
            // Получение информации о пользователе из ClaimsPrincipal
            var userId = int.Parse(httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var login = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? string.Empty;

            if (userId == 0)
            {
                return Results.Unauthorized();
            }

            // Проверка валидности сессии
            var session = await tokenValidationService.GetSessionByTokenAsync(token);

            if (session == null)
            {
                return Results.NotFound(new { error = "Session not found or expired" });
            }

            // Завершение сессии
            await sessionService.LogoutAsync(token, userId, login);

            return Results.Ok(new { message = "Session revoked successfully" });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static string? ExtractTokenFromHeader(IHeaderDictionary headers)
    {
        if (!headers.TryGetValue("Authorization", out var authHeader))
            return null;

        var authHeaderValue = authHeader.ToString();
        if (string.IsNullOrEmpty(authHeaderValue))
            return null;

        const string bearerPrefix = "Bearer ";
        if (!authHeaderValue.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        return authHeaderValue.Substring(bearerPrefix.Length).Trim();
    }
}