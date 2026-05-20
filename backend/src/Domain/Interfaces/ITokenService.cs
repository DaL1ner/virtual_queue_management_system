namespace Domain.Interfaces;

/// <summary>
/// Сервис для генерации и верификации токенов сессии
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Генерирует новый токен сессии и его хэш
    /// </summary>
    /// <returns>Кортеж (токен, хэш токена)</returns>
    (string Token, string Hash) GenerateSessionToken();

    /// <summary>
    /// Хэширует токен для безопасного хранения
    /// </summary>
    /// <param name="token">Оригинальный токен</param>
    /// <returns>Хэш токена</returns>
    string HashToken(string token);

    /// <summary>
    /// Проверяет, соответствует ли токен хэшу
    /// </summary>
    /// <param name="token">Оригинальный токен</param>
    /// <param name="hash">Хэш для проверки</param>
    /// <returns>True если соответствует</returns>
    bool VerifyToken(string token, string hash);
}