namespace Api.Endpoints;

using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public static class TicketEndpoints
{
    public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointGroup = app.MapGroup("/api/tickets").WithTags("Ticket");

        endpointGroup.MapGet("/", GetAllTickets);
        endpointGroup.MapGet("/{id:int}", GetTicketById);
        endpointGroup.MapPost("/{id:int}/move-backward", MoveTicketBackward);
        endpointGroup.MapGet("/{id:int}/position", GetTicketPosition);

        return endpointGroup;
    }

    /// <summary>
    /// Получить список всех талонов (без сортировки) для указанной сессии очереди
    /// </summary>
    private static async Task<IResult> GetAllTickets(
        [AsParameters] TicketQueryParams queryParams,
        TicketService service)
    {
        if (queryParams.QueueSessionId == null)
            return Results.BadRequest("Не указан queueSessionId.");

        var tickets = await service.GetAllBySessionAsync(queryParams.QueueSessionId.Value, queryParams.Sorted ?? false);
        return Results.Ok(tickets);
    }

    /// <summary>
    /// Получить конкретный талон по ID
    /// </summary>
    private static async Task<IResult> GetTicketById(int id, TicketService service)
    {
        var ticket = await service.GetByIdAsync(id);
        if (ticket == null)
            return Results.NotFound();

        return Results.Ok(ticket);
    }

    /// <summary>
    /// Переместить талон на N шагов назад
    /// </summary>
    private static async Task<IResult> MoveTicketBackward(int id, MoveTicketBackwardDto dto, TicketService service)
    {
        try
        {
            var ticket = await service.MoveBackwardAsync(id, dto.Steps, dto.ActorUserId);
            return Results.Ok(ticket);
        }
        catch (NotFoundException ex)
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

    /// <summary>
    /// Получить позицию талона в очереди
    /// </summary>
    private static async Task<IResult> GetTicketPosition(int id, TicketService service)
    {
        try
        {
            var position = await service.GetPositionAsync(id);
            return Results.Ok(position);
        }
        catch (NotFoundException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (BadRequestException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Параметры запроса для списка талонов
    /// </summary>
    public record TicketQueryParams(int? QueueSessionId, bool? Sorted);
}