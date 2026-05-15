namespace Api.Endpoints;

using Application.Services;
using Application.DTOs;
using Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;

public static class QueueSessionEndpoints
{
    public static IEndpointRouteBuilder MapQueueSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointGroup = app.MapGroup("/api/queue-sessions").WithTags("QueueSession");

        endpointGroup.MapGet("/", GetAllSessions);
        endpointGroup.MapGet("/{id:int}", GetSessionById);
        endpointGroup.MapGet("/{id:int}/statistics", GetSessionStatistics);
        endpointGroup.MapGet("/active/service-types", GetActiveSessionServiceTypes);
        endpointGroup.MapPost("/", CreateSession);
        endpointGroup.MapPost("/{id:int}/status", ChangeSessionStatus);

        return endpointGroup;
    }

    private static async Task<IResult> GetAllSessions(
        QueueSessionService service,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var (items, totalCount) = await service.GetAllAsync(page, pageSize);

        return Results.Ok(new
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
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

    private static async Task<IResult> GetActiveSessionServiceTypes(
        QueueSessionService queueSessionService,
        ServiceTypeService serviceTypeService)
    {
        var session = await queueSessionService.GetActiveSessionAsync();
        if (session == null)
            return Results.NotFound();

        var serviceTypes = await serviceTypeService.GetAllAsync(session.QueueConfigId);

        var queueConfig = await queueSessionService.GetConfigByIdAsync(session.QueueConfigId);
        var serviceTypesAllowed = queueConfig?.IsServiceTypeEnabled ?? false;

        var response = new ActiveSessionServiceTypesResponseDto(
            session.Id,
            session.Status.ToString(),
            session.QueueConfigId,
            session.StartedAt,
            serviceTypes,
            serviceTypesAllowed
        );

        return Results.Ok(response);
    }

    private static async Task<IResult> CreateSession([FromBody] CreateQueueSessionDto request, QueueSessionService queueSessionService)
    {
        // TODO: Заменить на получение ID из контекста аутентификации
        // Временно используем ID пользователя admin (id = 1)
        try
        {
            var created = await queueSessionService.CreateFromConfigAsync(request.QueueConfigId, createdById: 1);
            return Results.Created("", created);
        }
        catch (NotFoundException ex) when (ex.Message.Contains("not found"))
        {
            return Results.NotFound(ex.Message);
        }
        catch (BadRequestException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private static async Task<IResult> ChangeSessionStatus(
        int id,
        [FromBody] UpdateQueueSessionStatusDto request,
        QueueSessionService service)
    {
        // TODO: Заменить на получение ID из контекста аутентификации
        // Временно используем ID пользователя admin (id = 1)
        try
        {
            var updated = await service.ChangeStatusAsync(id, request.Status, actorUserId: 1);
            return Results.Ok(updated);
        }
        catch (NotFoundException ex) when (ex.Message.Contains("not found"))
        {
            return Results.NotFound(ex.Message);
        }
        catch (BadRequestException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }
}
