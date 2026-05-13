namespace Api.Endpoints;

using Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public static class QueueSessionEndpoints
{
    public static IEndpointRouteBuilder MapQueueSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointGroup = app.MapGroup("/api/queue-sessions").WithTags("QueueSession");

        endpointGroup.MapGet("/{id:int}", GetSessionById);
        endpointGroup.MapGet("/{id:int}/statistics", GetSessionStatistics);
        endpointGroup.MapPost("/", CreateSession);
        endpointGroup.MapPost("/{id:int}/status", ChangeSessionStatus);

        return endpointGroup;
    }

    private static async Task<IResult> GetSessionById(int id, QueueSessionService service)
    {
        var session = await service.GetByIdAsync(id);
        if (session == null)
            return Results.NotFound();

        return Results.Ok(session);
    }

    private static async Task<IResult> GetSessionStatistics(int id, QueueSessionService service)
    {
        var stats = await service.GetStatisticsAsync(id);
        if (stats == null)
            return Results.NotFound();

        return Results.Ok(stats);
    }

    private static async Task<IResult> CreateSession(QueueConfigService queueConfigService, QueueSessionService queueSessionService)
    {
        var configs = await queueConfigService.GetAllAsync();
        var config = configs.FirstOrDefault();
        if (config == null)
            return Results.BadRequest("No active queue configuration found");

        var created = await queueSessionService.CreateFromConfigAsync(config.Id, createdById: 0);
        return Results.Created("", created);
    }

    private static async Task<IResult> ChangeSessionStatus(int id, QueueSessionService service)
    {
        var updated = await service.ChangeStatusAsync(id, newStatus: Domain.Enums.SessionStatus.Paused, actorUserId: 0);
        return Results.Ok(updated);
    }
}
