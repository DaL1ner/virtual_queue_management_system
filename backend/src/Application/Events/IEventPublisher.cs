namespace Application.Events;

using MediatR;

/// <summary>
/// Интерфейс для публикации доменных событий
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Публикует доменное событие
    /// </summary>
    Task PublishAsync<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : class, INotification;
}
