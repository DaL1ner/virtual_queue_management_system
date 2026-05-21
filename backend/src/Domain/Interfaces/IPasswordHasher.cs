namespace Domain.Interfaces;

/// <summary>
/// Сервис для хеширования и верификации паролей
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Хеширует пароль
    /// </summary>
    /// <param name="password">Пароль в открытом виде</param>
    /// <returns>Хэш пароля</returns>
    string HashPassword(string password);

    /// <summary>
    /// Проверяет, соответствует ли пароль хэшу
    /// </summary>
    /// <param name="password">Пароль в открытом виде</param>
    /// <param name="hash">Хэш для проверки</param>
    /// <returns>True если пароль верный</returns>
    bool VerifyPassword(string password, string hash);
}