namespace Api.Endpoints;

using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;

public static class ExecutorStateEndpoints
{
    public static IEndpointRouteBuilder MapExecutorStateEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointGroup = app.MapGroup("/api/executor-states").WithTags("ExecutorState");

        endpointGroup.MapPost("/ready", ToggleReady);
        endpointGroup.MapGet("/", GetAllBySession);
        endpointGroup.MapGet("/{userId:int}", GetByUser);
        endpointGroup.MapPost("/call-next", CallNext);
        endpointGroup.MapPost("/mark-no-show", MarkNoShow);
        endpointGroup.MapPost("/start-serving", StartServing);

        return endpointGroup;
    }

    /// <summary>
    /// Переключение готовности исполнителя (toggle)
    /// </summary>
    private static async Task<IResult> ToggleReady(
        [FromBody] ToggleExecutorReadyDto dto,
        ExecutorStateService service,
        HttpContext httpContext)
    {
        try
        {
            // Валидация входных данных
            if (dto.UserId <= 0)
                return Results.BadRequest("UserId должен быть положительным числом.");

            // Извлечение actorUserId из контекста аутентификации (пока не реализовано)
            int? actorUserId = null;
            // TODO: когда появится аутентификация, брать из httpContext.User

            var result = await service.ToggleReadyAsync(dto, actorUserId);
            return Results.Ok(result);
        }
        catch (NotFoundException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (BadRequestException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 403);
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
    /// Получение всех состояний исполнителей для активной сессии
    /// </summary>
    private static async Task<IResult> GetAllBySession(
        ExecutorStateService service,
        QueueSessionService queueSessionService)
    {
        try
        {
            var activeSession = await queueSessionService.GetActiveSessionAsync();
            if (activeSession == null)
                return Results.BadRequest("Нет активной сессии очереди.");

            var states = await service.GetAllBySessionAsync(activeSession.Id);
            return Results.Ok(states);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Внутренняя ошибка сервера: {ex.Message}");
        }
    }

    /// <summary>
    /// Получение состояния исполнителя по ID пользователя для активной сессии
    /// </summary>
    private static async Task<IResult> GetByUser(
        int userId,
        ExecutorStateService service,
        QueueSessionService queueSessionService)
    {
        try
        {
            var activeSession = await queueSessionService.GetActiveSessionAsync();
            if (activeSession == null)
                return Results.BadRequest("Нет активной сессии очереди.");

            var state = await service.GetBySessionAndUserAsync(activeSession.Id, userId);
            if (state == null)
                return Results.NotFound($"Состояние исполнителя для пользователя {userId} не найдено.");

            return Results.Ok(state);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Внутренняя ошибка сервера: {ex.Message}");
        }
    }

    /// <summary>
    /// Вызов следующего талона (первого в очереди) и назначение исполнителя
    /// </summary>
    private static async Task<IResult> CallNext(
        [FromBody] CallNextTicketDto dto,
        ExecutorStateService executorStateService,
        HttpContext httpContext)
    {
        try
        {
            // TODO: извлечение actorUserId из контекста аутентификации
            int? actorUserId = null;
            
            var result = await executorStateService.CallNextTicketAsync(dto, actorUserId);
            return Results.Ok(result);
        }
        catch (NotFoundException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (BadRequestException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 400);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Внутренняя ошибка сервера: {ex.Message}");
        }
    }

    /// <summary>
    /// Фиксация неявки клиента (перевод талона в статус Skipped и освобождение исполнителя)
    /// </summary>
    private static async Task<IResult> MarkNoShow(
        [FromBody] MarkNoShowDto dto,
        ExecutorStateService service,
        HttpContext httpContext)
    {
        try
        {
            // Извлечение actorUserId из контекста аутентификации (пока не реализовано)
            int? actorUserId = null;
            // TODO: когда появится аутентификация, брать из httpContext.User

            var result = await service.MarkNoShowAsync(dto, actorUserId);
            return Results.Ok(result);
        }
        catch (NotFoundException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (BadRequestException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 403);
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
    /// Начало обслуживания текущего талона исполнителем
    /// </summary>
    private static async Task<IResult> StartServing(
        [FromBody] StartServingDto dto,
        ExecutorStateService service,
        HttpContext httpContext)
    {
        try
        {
            // Извлечение actorUserId из контекста аутентификации (пока не реализовано)
            int? actorUserId = null;
            // TODO: когда появится аутентификация, брать из httpContext.User

            var result = await service.StartServingAsync(dto, actorUserId);
            return Results.Ok(result);
        }
        catch (NotFoundException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (BadRequestException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 403);
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
}
