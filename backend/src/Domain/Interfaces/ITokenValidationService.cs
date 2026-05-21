using System.Threading.Tasks;
using Domain.DTOs;

namespace Domain.Interfaces;

/// <summary>
/// Сервис валидации токенов аутентификации
/// </summary>
public interface ITokenValidationService
{
    /// <summary>
    /// Валидирует токен и возвращает результат аутентификации
    /// </summary>
    /// <param name="token">Токен (GUID)</param>
    /// <returns>AuthenticationResult или null, если токен невалиден</returns>
    Task<AuthenticationResult?> ValidateTokenAsync(string token);
}