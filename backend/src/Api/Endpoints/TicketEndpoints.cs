namespace Api.Endpoints;

using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

public static class TicketEndpoints
{
    public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointGroup = app.MapGroup("/api/tickets").WithTags("Ticket");

        endpointGroup.MapGet("/{ticketId:int}", GetTicketById);
        endpointGroup.MapPost("/", CreateTicket);
        endpointGroup.MapPost("/{ticketId:int}/cancel", CancelTicket);
        endpointGroup.MapPost("/{ticketId:int}/move-backward", MoveTicketBackward);
        endpointGroup.MapPost("/{ticketId:int}/move-to-position", MoveTicketToPosition);
        endpointGroup.MapGet("/{ticketId:int}/position", GetTicketPosition);
        endpointGroup.MapGet("/queue", GetQueue);

        // Отдельная группа для получения всех талонов
        var allEndpointGroup = app.MapGroup("/api/tickets/all").WithTags("Ticket");
        allEndpointGroup.MapGet("/", GetAllTickets);

        return endpointGroup;
    }

    /// <summary>
    /// Получить список всех талонов указанной сессии очереди (или активной, если queueSessionId не указан)
    /// Сортировка по умолчанию: по ID (ascending). При sorted=true: по PriorityLevel DESC, SortOrder ASC, CreatedAt ASC
    /// </summary>
    private static async Task<IResult> GetAllTickets(
        TicketService service,
        QueueSessionService queueSessionService,
        [FromQuery] int? queueSessionId = null,
        [FromQuery] bool sorted = false)
    {
        try
        {
            var tickets = await service.GetAllBySessionAsync(queueSessionId, queueSessionService, sorted);
            return Results.Ok(tickets);
        }
        catch (NotFoundException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (BadRequestException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Внутренняя ошибка сервера: {ex.Message}");
        }
    }

    /// <summary>
    /// Создать новый талон (публичный эндпоинт для клиентов)
    /// </summary>
    private static async Task<IResult> CreateTicket(
        CreateTicketWithDeviceDto dto,
        TicketService ticketService,
        ClientSessionService clientSessionService,
        HttpContext httpContext)
    {
        try
        {
            // Валидация входных данных
            if (string.IsNullOrWhiteSpace(dto.DeviceFingerprint))
                return Results.BadRequest("DeviceFingerprint обязателен.");
            if (string.IsNullOrWhiteSpace(dto.ClientName))
                return Results.BadRequest("ClientName обязателен.");
            if (string.IsNullOrWhiteSpace(dto.ClientSurname))
                return Results.BadRequest("ClientSurname обязателен.");

            // Извлечение IP-адреса и User-Agent из контекста, если не переданы в DTO
            var ipAddress = dto.IpAddress ?? httpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = dto.UserAgent ?? httpContext.Request.Headers.UserAgent.ToString();

            // Создание или получение клиентской сессии
            var clientSessionDto = await clientSessionService.GetOrCreateAsync(new CreateClientSessionDto(
                dto.DeviceFingerprint,
                string.Empty, // TokenHash placeholder - будет сгенерирован при реализации генерации токенов
                ipAddress,
                userAgent
            ));

            // Создание талона
            var ticketDto = await ticketService.CreateAsync(
                new CreateTicketDto(dto.ClientName, dto.ClientSurname, dto.ServiceTypeId),
                clientSessionDto.Id,
                actorUserId: null // Публичный эндпоинт, actorUserId = null
            );

            return Results.Created($"/api/tickets/{ticketDto.Id}", ticketDto);
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
        catch (Exception ex)
        {
            // Логирование внутренней ошибки
            return Results.Problem($"Внутренняя ошибка сервера: {ex.Message}");
        }
    }

    /// <summary>
    /// Получить детальную информацию о талоне по ID (с расчётом времени ожидания)
    /// </summary>
    private static async Task<IResult> GetTicketById(int ticketId, TicketService service)
    {
        var ticket = await service.GetDetailAsync(ticketId);
        if (ticket == null)
            return Results.NotFound();

        return Results.Ok(ticket);
    }

    /// <summary>
    /// Переместить талон на N шагов назад
    /// </summary>
    private static async Task<IResult> MoveTicketBackward(int ticketId, MoveTicketBackwardDto dto, TicketService service)
    {
        try
        {
            var ticket = await service.MoveBackwardAsync(ticketId, dto.Steps, dto.ActorUserId);
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
    /// Переместить талон в целевую позицию в очереди (абсолютный индекс, 1 = начало очереди)
    /// </summary>
    private static async Task<IResult> MoveTicketToPosition(int ticketId, MoveTicketToPositionDto dto, TicketService service)
    {
        try
        {
            var ticket = await service.MoveToPositionAsync(ticketId, dto.Position, dto.ActorUserId);
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
    private static async Task<IResult> GetTicketPosition(int ticketId, TicketService service)
    {
        try
        {
            var position = await service.GetPositionAsync(ticketId);
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
    /// Получение списка ожидающих талонов (очереди) в активной сессии
    /// Сортировка: PriorityLevel DESC, SortOrder ASC, CreatedAt ASC
    /// </summary>
    private static async Task<IResult> GetQueue(TicketService service)
    {
        try
        {
            var tickets = await service.GetQueueAsync();
            return Results.Ok(tickets);
        }
        catch (BadRequestException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Отмена талона (перевод из WAITING в CANCELLED)
    /// </summary>
    private static async Task<IResult> CancelTicket(
        int ticketId,
        TicketService service)
    {
        try
        {
            var ticket = await service.CancelTicketAsync(ticketId, actorUserId: null);
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
    /// Параметры запроса для списка талонов
    /// </summary>
    public record TicketQueryParams(bool? Sorted);
}
