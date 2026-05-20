namespace Infrastructure.Security;

using System.Security.Cryptography;
using System.Text;
using Domain.Interfaces;

/// <summary>
/// Реализация сервиса токенов с использованием GUID и SHA256
/// </summary>
public class TokenService : ITokenService
{
    /// <summary>
    /// Генерирует новый токен сессии (GUID) и его хэш
    /// </summary>
    public (string Token, string Hash) GenerateSessionToken()
    {
        var token = Guid.NewGuid().ToString("N");
        var hash = HashToken(token);
        return (token, hash);
    }

    /// <summary>
    /// Хэширует токен с помощью SHA256
    /// </summary>
    public string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Проверяет, соответствует ли токен хэшу
    /// </summary>
    public bool VerifyToken(string token, string hash)
    {
        var computedHash = HashToken(token);
        return string.Equals(computedHash, hash, StringComparison.Ordinal);
    }
}