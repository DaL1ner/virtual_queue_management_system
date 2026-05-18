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
    private readonly ILogger<ExecutorStateService> _logger;

    public ExecutorStateService(
        AppDbContext context,
        IEventPublisher eventPublisher,
        QueueSessionService queueSessionService,
        UserService userService,
        ILogger<ExecutorStateService> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _queueSessionService = queueSessionService;
        _userService = userService;
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
