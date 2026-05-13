namespace Application.DependencyInjection;

using Application.Services;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<EventLogService>();
        services.AddScoped<ClientSessionService>();
        services.AddScoped<QueueSessionService>();
        services.AddScoped<QueueConfigService>();

        return services;
    }
}
