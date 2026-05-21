namespace Api.Endpoints;

using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointGroup = app.MapGroup("/api/auth").WithTags("Auth");

        endpointGroup.MapPost("/login", Login);

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
}