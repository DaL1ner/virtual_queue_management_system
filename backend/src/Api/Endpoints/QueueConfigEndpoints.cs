namespace Api.Endpoints;

using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using Api.Helpers;

public static class QueueConfigEndpoints
{
    public static IEndpointRouteBuilder MapQueueConfigEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointGroup = app.MapGroup("/api/queue-configs").WithTags("QueueConfig");

        endpointGroup.MapGet("/", GetAllQueueConfigs);
        endpointGroup.MapGet("/{id:int}", GetQueueConfigById);
        endpointGroup.MapPost("/", CreateQueueConfig);
        endpointGroup.MapPut("/{id:int}", UpdateQueueConfig);

        return endpointGroup;
    }

    private static async Task<IResult> GetAllQueueConfigs(QueueConfigService service)
    {
        var configs = await service.GetAllAsync();
        return Results.Ok(configs);
    }

    private static async Task<IResult> GetQueueConfigById(int id, QueueConfigService service)
    {
        var config = await service.GetByIdAsync(id);
        if (config == null)
            return Results.NotFound();

        return Results.Ok(config);
    }

    private static async Task<IResult> CreateQueueConfig(
        CreateQueueConfigDto dto,
        ClaimsPrincipal user,
        QueueConfigService service)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();
            
        if (!user.IsInRole("ADMIN"))
            return Results.Forbid();
            
        var created = await service.CreateAsync(dto, createdById: userId.Value);
        return Results.Created("", created);
    }

    private static async Task<IResult> UpdateQueueConfig(
        int id,
        UpdateQueueConfigDto dto,
        ClaimsPrincipal user,
        QueueConfigService service)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();
            
        if (!user.IsInRole("ADMIN"))
            return Results.Forbid();
            
        var updated = await service.UpdateAsync(id, dto, actorUserId: userId.Value);
        return Results.Ok(updated);
    }
}
