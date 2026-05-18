namespace Application.Services;

using System.Data.Common;
using Domain.Entities;
using Domain.Enums;
using Application.DTOs;
using Application.Events;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Сервис для управления состоянием исполнителей (executor_states)
/// </summary>
public class ExecutorStateService
{
    private readonly AppDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly QueueSessionService _queueSessionService;
    private readonly UserService _userService;
    private readonly TicketService _ticketService;
    private readonly ILogger<ExecutorStateService> _logger;

    public ExecutorStateService(
        AppDbContext context,
        IEventPublisher eventPublisher,
        QueueSessionService queueSessionService,
        UserService userService,
        TicketService ticketService,
        ILogger<ExecutorStateService> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _queueSessionService = queueSessionService;
        _userService = userService;
        _ticketService = ticketService;
        _logger = logger;
    }

    /// <summary>
    /// Переключение готовности исполнителя (toggle)
    /// </summary>
    /// <param name="dto">DTO с идентификатором пользователя и опционально сессии</param>
    /// <param name="actorUserId">ID пользователя, инициировавшего изменение (опционально)</param>
    /// <returns>DTO обновлённого состояния исполнителя</returns>
    public async Task<ExecutorStateDto> ToggleReadyAsync(ToggleExecutorReadyDto dto, int? actorUserId = null)
    {
        // 1. Проверка существования пользователя
        var user = await _userService.GetByIdAsync(dto.UserId);
        _logger.LogInformation("ToggleReadyAsync: пользователь {UserId} существует", dto.UserId);

        // 1.5 Проверка наличия роли EXECUTOR
        var hasExecutorRole = await _userService.HasRoleAsync(dto.UserId, "EXECUTOR");
        if (!hasExecutorRole)
            throw new UnauthorizedAccessException(
                $"Пользователь {dto.UserId} не имеет роли EXECUTOR и не может управлять состоянием исполнителя.");

        // 2. Определение сессии очереди
        int queueSessionId;
        if (dto.QueueSessionId.HasValue)
        {
            queueSessionId = dto.QueueSessionId.Value;
            // Проверим, что сессия существует
            var sessionExists = await _context.QueueSessions.AnyAsync(qs => qs.Id == queueSessionId);
            if (!sessionExists)
                throw new NotFoundException($"Сессия очереди с ID {queueSessionId} не найдена.");
        }
        else
        {
            var activeSession = await _queueSessionService.GetActiveSessionAsync();
            if (activeSession == null)
                throw new BadRequestException("Нет активной сессии очереди.");
            queueSessionId = activeSession.Id;
        }

        _logger.LogInformation("ToggleReadyAsync: используем сессию {QueueSessionId}", queueSessionId);

        // 3. Получение или создание состояния исполнителя
        var executorState = await GetOrCreateAsync(queueSessionId, dto.UserId);

        // 4. Сохраняем старое значение для события
        var oldIsReady = executorState.IsReady;
        var newIsReady = !oldIsReady;

        // 5. Проверка ограничения: если новый статус "готов", текущий талон должен быть null
        if (newIsReady && executorState.CurrentTicketId.HasValue)
        {
            throw new BadRequestException(
                "Исполнитель не может быть помечен как готовый, пока у него есть текущий талон. " +
                "Завершите обслуживание текущего талона сначала.");
        }

        // 6. Обновление состояния
        executorState.IsReady = newIsReady;
        executorState.LastStatusChange = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "ToggleReadyAsync: состояние исполнителя {ExecutorStateId} изменено с {OldIsReady} на {NewIsReady}",
            executorState.Id, oldIsReady, newIsReady);

        // 7. Публикация события
        await _eventPublisher.PublishAsync(new ExecutorStateChangedEvent(
            executorState.Id,
            queueSessionId,
            dto.UserId,
            oldIsReady,
            newIsReady,
            actorUserId));

        // 8. Возврат DTO
        return await MapToDtoAsync(executorState);
    }

    /// <summary>
    /// Получение состояния исполнителя по сессии и пользователю (с созданием, если не существует)
    /// </summary>
    private async Task<ExecutorState> GetOrCreateAsync(int queueSessionId, int userId)
    {
        var existing = await _context.ExecutorStates
            .Include(es => es.User)
            .Include(es => es.QueueSession)
            .Include(es => es.CurrentTicket)
            .FirstOrDefaultAsync(es => es.QueueSessionId == queueSessionId && es.UserId == userId);

        if (existing != null)
            return existing;

        // Создание новой записи
        var newState = new ExecutorState
        {
            QueueSessionId = queueSessionId,
            UserId = userId,
            IsReady = false,
            CurrentTicketId = null,
            LastStatusChange = DateTime.UtcNow
        };

        _context.ExecutorStates.Add(newState);
        await _context.SaveChangesAsync();

        // Загружаем навигационные свойства
        await _context.Entry(newState)
            .Reference(es => es.User)
            .LoadAsync();
        await _context.Entry(newState)
            .Reference(es => es.QueueSession)
            .LoadAsync();

        _logger.LogInformation(
            "GetOrCreateAsync: создано новое состояние исполнителя для пользователя {UserId} в сессии {QueueSessionId}",
            userId, queueSessionId);

        return newState;
    }

    /// <summary>
    /// Получение состояния исполнителя по сессии и пользователю (без создания)
    /// </summary>
    public async Task<ExecutorStateDto?> GetBySessionAndUserAsync(int queueSessionId, int userId)
    {
        var state = await _context.ExecutorStates
            .Include(es => es.User)
            .Include(es => es.QueueSession)
            .Include(es => es.CurrentTicket)
            .FirstOrDefaultAsync(es => es.QueueSessionId == queueSessionId && es.UserId == userId);

        if (state == null)
            return null;

        return await MapToDtoAsync(state);
    }

    /// <summary>
    /// Получение всех состояний исполнителей для указанной сессии
    /// </summary>
    public async Task<IEnumerable<ExecutorStateDto>> GetAllBySessionAsync(int queueSessionId)
    {
        var states = await _context.ExecutorStates
            .Include(es => es.User)
            .Include(es => es.QueueSession)
            .Include(es => es.CurrentTicket)
            .Where(es => es.QueueSessionId == queueSessionId)
            .ToListAsync();

        var dtos = new List<ExecutorStateDto>();
        foreach (var state in states)
        {
            dtos.Add(await MapToDtoAsync(state));
        }
        return dtos;
    }

    /// <summary>
    /// Вызов следующего талона (первого в очереди) и назначение исполнителя
    /// </summary>
    public async Task<CallNextTicketResponseDto> CallNextTicketAsync(CallNextTicketDto dto, int? actorUserId = null)
    {
        // 1. Получить активную сессию очереди
        var activeSession = await _queueSessionService.GetActiveSessionAsync();
        if (activeSession == null)
            throw new BadRequestException("Нет активной сессии очереди.");

        // 2. Найти первый талон в статусе WAITING для этой сессии
        var ticket = await GetFirstWaitingTicketAsync(activeSession.Id);
        if (ticket == null)
            throw new BadRequestException("Нет ожидающих талонов в текущей сессии.");

        // 3. Выбрать исполнителя
        var executorState = await GetRandomReadyExecutorAsync(activeSession.Id, dto.ExecutorUserId);

        // 4. Обновить статус талона на CALLED через TicketService
        var ticketDto = await _ticketService.ChangeStatusAsync(
            ticket.Id,
            TicketStatus.Called,
            actorUserId,
            executorState.UserId); // executorUserId передаётся как исполнитель

        // 5. Обновить состояние исполнителя (привязать талон, снять готовность)
        var oldIsReady = executorState.IsReady;
        executorState.CurrentTicketId = ticket.Id;
        executorState.IsReady = false;
        executorState.LastStatusChange = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // 6. Публикация события изменения состояния исполнителя
        await _eventPublisher.PublishAsync(new ExecutorStateChangedEvent(
            executorState.Id,
            activeSession.Id,
            executorState.UserId,
            oldIsReady,
            false,
            actorUserId));

        // 7. Возврат DTO ответа
        return new CallNextTicketResponseDto(
            ticketDto,
            executorState.UserId,
            executorState.User?.FullName ?? "Неизвестно"
        );
    }

    /// <summary>
    /// Получение первого ожидающего талона в сессии
    /// </summary>
    private async Task<Ticket?> GetFirstWaitingTicketAsync(int queueSessionId)
    {
        return await _context.Tickets
            .Where(t => t.QueueSessionId == queueSessionId && t.Status == TicketStatus.Waiting)
            .OrderByDescending(t => t.PriorityLevel)
            .ThenBy(t => t.SortOrder)
            .ThenBy(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Получение случайного готового исполнителя (или указанного)
    /// </summary>
    private async Task<ExecutorState> GetRandomReadyExecutorAsync(int queueSessionId, int? preferredExecutorUserId = null)
    {
        var query = _context.ExecutorStates
            .Where(es => es.QueueSessionId == queueSessionId
                         && es.IsReady
                         && es.CurrentTicketId == null);

        if (preferredExecutorUserId.HasValue)
        {
            var preferred = await query.FirstOrDefaultAsync(es => es.UserId == preferredExecutorUserId.Value);
            if (preferred == null)
                throw new BadRequestException($"Указанный исполнитель {preferredExecutorUserId} не готов или уже имеет текущий талон.");
            return preferred;
        }

        // Случайный выбор из готовых исполнителей
        var readyExecutors = await query.ToListAsync();
        if (!readyExecutors.Any())
            throw new BadRequestException("Нет готовых исполнителей без текущего талона.");

        var randomIndex = new Random().Next(0, readyExecutors.Count);
        return readyExecutors[randomIndex];
    }

    /// <summary>
    /// Маппинг сущности в DTO
    /// </summary>
    private async Task<ExecutorStateDto> MapToDtoAsync(ExecutorState state)
    {
        // Вычисление количества обслуженных талонов
        var totalServedCount = await _context.Tickets
            .CountAsync(t => t.ServedByUserId == state.UserId && t.Status == Domain.Enums.TicketStatus.Served);

        // Вычисление среднего времени обслуживания (в секундах)
        double? avgServiceTimeSec = null;
        var servedTickets = await _context.Tickets
            .Where(t => t.ServedByUserId == state.UserId && t.Status == Domain.Enums.TicketStatus.Served &&
                        t.ServiceStartedAt.HasValue && t.ServiceEndedAt.HasValue)
            .Select(t => new { t.ServiceStartedAt, t.ServiceEndedAt })
            .ToListAsync();

        if (servedTickets.Any())
        {
            avgServiceTimeSec = servedTickets
                .Average(t => (t.ServiceEndedAt!.Value - t.ServiceStartedAt!.Value).TotalSeconds);
        }

        return new ExecutorStateDto(
            state.Id,
            state.QueueSessionId,
            state.UserId,
            state.User?.FullName ?? "Неизвестно",
            state.IsReady,
            state.CurrentTicketId,
            state.CurrentTicket?.TicketNumber,
            state.LastStatusChange,
            totalServedCount,
            avgServiceTimeSec
        );
    }
}
