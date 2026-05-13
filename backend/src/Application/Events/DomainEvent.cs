namespace Application.Events;

using MediatR;

/// <summary>
/// Базовый класс для всех доменных событий
/// </summary>
public abstract class DomainEvent : INotification
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
