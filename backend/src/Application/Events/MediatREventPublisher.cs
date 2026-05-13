namespace Application.Events;

using MediatR;

/// <summary>
/// Реализация IEventPublisher на базе MediatR
/// </summary>
public class MediatREventPublisher : IEventPublisher
{
    private readonly IMediator _mediator;

    public MediatREventPublisher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task PublishAsync<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : class, INotification
    {
        await _mediator.Publish(domainEvent);
    }
}
