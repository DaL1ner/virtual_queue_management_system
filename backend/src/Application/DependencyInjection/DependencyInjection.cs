namespace Application.DependencyInjection;

using Application.Services;
using Application.Events;
using Microsoft.Extensions.DependencyInjection;
using MediatR;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR - register handlers and publisher
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // Event Publisher
        services.AddScoped<IEventPublisher, MediatREventPublisher>();

        // Event Log Handler
        services.AddScoped<EventLogDomainEventHandler>();

        // Other services
        services.AddScoped<ClientSessionService>();
        services.AddScoped<QueueSessionService>();
        services.AddScoped<QueueConfigService>();
        services.AddScoped<ServiceTypeService>();
        services.AddScoped<UserService>();

        // Event Log Handlers
        services.AddScoped<UserLogDomainEventHandler>();

        return services;
    }
}
