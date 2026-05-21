using Microsoft.AspNetCore.Builder;

namespace Api.Middleware;

/// <summary>
/// Методы расширения для регистрации AuthenticationMiddleware
/// </summary>
public static class AuthenticationMiddlewareExtensions
{
    /// <summary>
    /// Добавляет middleware аутентификации в конвейер обработки запросов
    /// </summary>
    public static IApplicationBuilder UseAuthenticationMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthenticationMiddleware>();
    }
}