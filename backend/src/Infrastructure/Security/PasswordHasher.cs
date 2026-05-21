namespace Infrastructure.Security;

using System.Security.Cryptography;
using System.Text;
using Domain.Interfaces;

/// <summary>
/// Реализация хешера паролей с использованием SHA256 и соли
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const string Salt = "virtual-queue-salt-2025"; // TODO: Вынести в конфигурацию

    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var saltedPassword = password + Salt;
        var bytes = Encoding.UTF8.GetBytes(saltedPassword);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyPassword(string password, string hash)
    {
        var computedHash = HashPassword(password);
        return string.Equals(computedHash, hash, StringComparison.Ordinal);
    }
}