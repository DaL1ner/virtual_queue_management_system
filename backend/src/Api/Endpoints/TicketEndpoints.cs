namespace Api.Endpoints;

using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using Api.Helpers;

public static class TicketEndpoints
{
    public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder app)
    {
        // Основная группа для операций с ticketId
        var ticketGroup = app.MapGroup("/api/tickets").WithTags("Ticket");

        ticketGroup.MapGet("/{ticketId:int}", GetTicketById);
        ticketGroup.MapPost("/", CreateTicket);
        ticketGroup.MapPost("/{ticketId:int}/cancel", CancelTicket);
        ticketGroup.MapPost("/{ticketId:int}/move-backward", MoveTicketBackward);
        ticketGroup.MapPost("/{ticketId:int}/move-to-position", MoveTicketToPosition);
        ticketGroup.MapGet("/{ticketId:int}/position", GetTicketPosition);
        ticketGroup.MapGet("/queue", GetQueue);

        // Отдельная группа для клиентских эндпоинтов (me)
        var meGroup = app.MapGroup("/api/tickets/me").WithTags("Ticket");

        meGroup.MapGet("/", GetMyActiveTicket);
        meGroup.MapPost("/cancel", CancelMyTicket);
        meGroup.MapPost("/move-backward", MoveMyTicketBackward);

        // Отдельная группа для получения всех талонов
        var allEndpointGroup = app.MapGroup("/api/tickets/all").WithTags("Ticket");
        allEndpointGroup.MapGet("/", GetAllTickets);

        return ticketGroup;
    }

    /// <summary>
    /// Получить список всех талонов указанной сессии очереди (или активной, если queueSessionId не указан)
    /// Сортировка по умолчанию: по ID (ascending). При sorted=true: по PriorityLevel DESC, SortOrder ASC, CreatedAt ASC
    /// </summary>
    private static async Task<IResult> GetAllTickets(
        ClaimsPrincipal user,
        TicketService service,
        QueueSessionService queueSessionService,
        [FromQuery] int? queueSessionId = null,
        [FromQuery] bool sorted = false)
    {
        // GET /api/tickets/all требует аутентификации и роли ADMIN или OPERATOR
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();
            
        if (!user.IsInAnyRole("ADMIN", "OPERATOR"))
            return Results.Forbid();
            
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

            // Возвращаем талон вместе с токеном сессии
            var response = new
            {
                Ticket = ticketDto,
                SessionToken = clientSessionDto.Token
            };

            return Results.Created($"/api/tickets/{ticketDto.Id}", response);
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
    /// Требует аутентификации и роли ADMIN или OPERATOR
    /// </summary>
    private static async Task<IResult> GetTicketById(
        int ticketId,
        ClaimsPrincipal user,
        TicketService service)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();

        if (!user.IsInAnyRole("ADMIN", "OPERATOR"))
            return Results.Forbid();

        var ticket = await service.GetDetailAsync(ticketId);
        if (ticket == null)
            return Results.NotFound();

        return Results.Ok(ticket);
    }

    /// <summary>
    /// Переместить талон на N шагов назад
    /// </summary>
    private static async Task<IResult> MoveTicketBackward(
        int ticketId,
        MoveTicketBackwardDto dto,
        ClaimsPrincipal user,
        TicketService service)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();
            
        if (!user.IsInAnyRole("ADMIN", "OPERATOR"))
            return Results.Forbid();
            
        try
        {
            var ticket = await service.MoveBackwardAsync(ticketId, dto.Steps, actorUserId: userId.Value);
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
    private static async Task<IResult> MoveTicketToPosition(
        int ticketId,
        MoveTicketToPositionDto dto,
        ClaimsPrincipal user,
        TicketService service)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();
            
        if (!user.IsInAnyRole("ADMIN", "OPERATOR"))
            return Results.Forbid();
            
        try
        {
            var ticket = await service.MoveToPositionAsync(ticketId, dto.Position, actorUserId: userId.Value);
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
    /// Требует аутентификации и роли ADMIN или OPERATOR
    /// </summary>
    private static async Task<IResult> GetTicketPosition(
        int ticketId,
        ClaimsPrincipal user,
        TicketService service)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();

        if (!user.IsInAnyRole("ADMIN", "OPERATOR"))
            return Results.Forbid();

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
    /// Требует аутентификации и роли ADMIN или OPERATOR
    /// Сортировка: PriorityLevel DESC, SortOrder ASC, CreatedAt ASC
    /// </summary>
    private static async Task<IResult> GetQueue(
        ClaimsPrincipal user,
        TicketService service)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();

        if (!user.IsInAnyRole("ADMIN", "OPERATOR"))
            return Results.Forbid();

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
    /// Требует аутентификации и роли ADMIN или OPERATOR
    /// </summary>
    private static async Task<IResult> CancelTicket(
        int ticketId,
        ClaimsPrincipal user,
        TicketService service)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();

        if (!user.IsInAnyRole("ADMIN", "OPERATOR"))
            return Results.Forbid();
            
        try
        {
            var ticket = await service.CancelTicketAsync(ticketId, actorUserId: userId.Value);
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
    /// Переместить активный талон клиента на N шагов назад
    /// Доступно только для клиентов (entity_type = "client")
    /// </summary>
    private static async Task<IResult> MoveMyTicketBackward(
        MoveTicketBackwardDto dto,
        ClaimsPrincipal user,
        TicketService service)
    {
        var clientSessionId = user.GetClientSessionId();
        if (clientSessionId == null)
            return Results.Unauthorized();

        var ticket = await service.GetActiveTicketAsync(clientSessionId.Value);
        if (ticket == null)
            return Results.NotFound("У вас нет активных талонов в очереди.");

        try
        {
            var updatedTicket = await service.MoveBackwardAsync(ticket.Id, dto.Steps);
            return Results.Ok(updatedTicket);
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
    /// Отменить активный талон текущего клиента
    /// Доступно только для клиентов (entity_type = "client")
    /// </summary>
    private static async Task<IResult> CancelMyTicket(
        ClaimsPrincipal user,
        TicketService service)
    {
        var clientSessionId = user.GetClientSessionId();
        if (clientSessionId == null)
            return Results.Unauthorized();

        try
        {
            var ticket = await service.CancelMyTicketAsync(clientSessionId.Value);
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
    }

    /// <summary>
    /// Получить активный талон текущего клиента по session token
    /// Возвращает детальную информацию с estimatedWaitMinutes и totalWaiting
    /// Доступно только для клиентов (entity_type = "client")
    /// </summary>
    private static async Task<IResult> GetMyActiveTicket(
        ClaimsPrincipal user,
        TicketService service)
    {
        var clientSessionId = user.GetClientSessionId();
        if (clientSessionId == null)
            return Results.Unauthorized();

        var ticket = await service.GetMyActiveTicketDetailAsync(clientSessionId.Value);
        if (ticket == null)
            return Results.NotFound("У вас нет активных талонов в очереди.");

        return Results.Ok(ticket);
    }

    /// <summary>
    /// Параметры запроса для списка талонов
    /// </summary>
    public record TicketQueryParams(bool? Sorted);
}
