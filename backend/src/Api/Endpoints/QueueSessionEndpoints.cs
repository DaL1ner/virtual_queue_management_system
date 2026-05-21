namespace Api.Endpoints;

using Application.Services;
using Application.DTOs;
using Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Api.Helpers;

public static class QueueSessionEndpoints
{
    public static IEndpointRouteBuilder MapQueueSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointGroup = app.MapGroup("/api/queue-sessions").WithTags("QueueSession");

        endpointGroup.MapGet("/", GetAllSessions);
        endpointGroup.MapGet("/{id:int}", GetSessionById);
        endpointGroup.MapGet("/statistics/active", GetActiveSessionStatistics);
        endpointGroup.MapGet("/statistics/{id:int?}", GetSessionStatistics);
        endpointGroup.MapGet("/active/service-types", GetActiveSessionServiceTypes);
        endpointGroup.MapPost("/", CreateSession);
        endpointGroup.MapPost("/{id:int}/status", ChangeSessionStatus);

        return endpointGroup;
    }

    private static async Task<IResult> GetAllSessions(
        ClaimsPrincipal user,
        QueueSessionService service,
        [FromQuery] bool isActive = false)
    {
        if (!user.IsInRole("ADMIN"))
            return Results.Forbid();

        var sessions = await service.GetAllAsync(filterActive: isActive);

        return Results.Ok(sessions);
    }

    private static async Task<IResult> GetSessionById(
        int id,
        ClaimsPrincipal user,
        QueueSessionService service)
    {
        if (!user.IsInRole("ADMIN"))
            return Results.Forbid();
            
        var session = await service.GetByIdAsync(id);
        if (session == null)
            return Results.NotFound();

        return Results.Ok(session);
    }

    private static async Task<IResult> GetSessionStatistics(
        int? id,
        ClaimsPrincipal user,
        QueueSessionService service)
    {
        if (user.IsClient())
            return Results.Forbid();
            
        QueueSessionStatsDto stats;

        if (id.HasValue)
        {
            stats = await service.GetStatisticsAsync(id.Value);
        }
        else
        {
            stats = await service.GetStatisticsAsync();
        }

        return Results.Ok(stats);
    }

    private static async Task<IResult> GetActiveSessionStatistics(
        ClaimsPrincipal user,
        QueueSessionService service)
    {
        if (user.IsClient())
            return Results.Forbid();

        var stats = await service.GetStatisticsAsync();

        return Results.Ok(stats);
    }

    private static async Task<IResult> GetActiveSessionServiceTypes(
        ClaimsPrincipal user,
        QueueSessionService queueSessionService,
        ServiceTypeService serviceTypeService)
    {
        var session = await queueSessionService.GetActiveSessionAsync();
        if (session == null)
            return Results.NotFound();

        var serviceTypes = await serviceTypeService.GetAllAsync(session.QueueConfigId);

        if (user.IsInRole("ADMIN"))
        {
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

        var simpleResponse = serviceTypes.Select(st => new ServiceTypeSimpleDto(st.Id, st.Name));
        return Results.Ok(simpleResponse);
    }

    private static async Task<IResult> CreateSession(
        [FromBody] CreateQueueSessionDto request,
        ClaimsPrincipal user,
        QueueSessionService queueSessionService)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();
            
        if (!user.IsInRole("ADMIN"))
            return Results.Forbid();
            
        try
        {
            var created = await queueSessionService.CreateFromConfigAsync(request.QueueConfigId, createdById: userId.Value);
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
        ClaimsPrincipal user,
        QueueSessionService queueSessionService,
        TicketService ticketService,
        ClientSessionService clientSessionService)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();
            
        if (!user.IsInRole("ADMIN"))
            return Results.Forbid();
            
        try
        {
            var session = await queueSessionService.GetByIdAsync(id);
            if (session == null)
                return Results.NotFound();

            var oldStatus = session.Status;

            // При переходе в CLOSED: закрыть талоны и инвалидировать клиентские сессии
            if (request.Status == SessionStatus.Closed && oldStatus != SessionStatus.Closed)
            {
                // 1. Закрыть все активные талоны сессии
                await ticketService.CloseAllTicketsForSessionAsync(id, actorUserId: userId.Value);

                // 2. Инвалидировать все клиентские сессии, привязанные к этой очереди
                await clientSessionService.InvalidateAllByQueueSessionAsync(id, actorUserId: userId.Value);
            }

            // 3. Изменить статус сессии (без инвалидации — она уже выполнена выше)
            var updated = await queueSessionService.ChangeStatusAsync(id, request.Status, actorUserId: userId.Value);
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
        catch (ConflictException ex)
        {
            return Results.Conflict(ex.Message);
        }
    }
}
