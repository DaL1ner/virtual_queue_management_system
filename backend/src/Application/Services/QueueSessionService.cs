namespace Application.Services;

using Domain.Entities;
using Domain.Enums;
using Application.DTOs;
using Application.Events;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Сервис для управления жизненным циклом сессий очередей
/// </summary>
public class QueueSessionService
{
    private readonly AppDbContext _context;
    private readonly IEventPublisher _eventPublisher;

    public QueueSessionService(
        AppDbContext context,
        IEventPublisher eventPublisher)
    {
        _context = context;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Возвращает активную сессию (Status = OPEN) для конфигурации
    /// </summary>
    public async Task<QueueSessionDto?> GetActiveSessionByConfigIdAsync(int configId)
    {
        var session = await _context.QueueSessions
            .Include(q => q.QueueConfig)
            .Include(q => q.CreatedBy)
            .Include(q => q.Tickets)
            .Include(q => q.ExecutorStates)
            .FirstOrDefaultAsync(q => q.QueueConfigId == configId && q.Status == SessionStatus.Open);

        if (session == null)
            return null;

        return MapToDto(session);
    }

    /// <summary>
    /// Получить активную сессию (Status = OPEN) в системе
    /// В системе может быть только одна активная сессия
    /// </summary>
    public async Task<QueueSession?> GetActiveSessionAsync()
    {
        var session = await _context.QueueSessions
            .Include(q => q.QueueConfig)
            .Include(q => q.CreatedBy)
            .Include(q => q.Tickets)
            .Include(q => q.ExecutorStates)
            .FirstOrDefaultAsync(q => q.Status == SessionStatus.Open);

        return session;
    }

    /// <summary>
    /// Возвращает конфигурацию очереди по ID
    /// </summary>
    public async Task<QueueConfig?> GetConfigByIdAsync(int configId)
    {
        return await _context.QueueConfigs
            .FirstOrDefaultAsync(qc => qc.Id == configId);
    }

    /// <summary>
    /// Создание сессии из конфигурации
    /// </summary>
    public async Task<QueueSessionDto> CreateFromConfigAsync(int configId, int createdById)
    {
        // Проверка что конфигурация существует и активна
        var config = await _context.QueueConfigs
            .Include(q => q.CreatedBy)
            .FirstOrDefaultAsync(q => q.Id == configId && q.IsActive);

        if (config == null)
        {
            throw new NotFoundException($"QueueConfig with id {configId} not found");
        }

        // Проверка что нет активной сессии в системе (OPEN или PAUSED)
        var existingActiveSession = await _context.QueueSessions
            .AnyAsync(q => q.Status == SessionStatus.Open || q.Status == SessionStatus.Paused);

        if (existingActiveSession)
        {
            throw new BadRequestException("There is already an active session in the system");
        }

        var session = new QueueSession
        {
            QueueConfigId = configId,
            Status = SessionStatus.Draft,
            CreatedById = createdById
        };

        _context.QueueSessions.Add(session);
        await _context.SaveChangesAsync();

        // Создание нативной последовательности PostgreSQL
        await _context.Database.ExecuteSqlRawAsync(
            $"CREATE SEQUENCE IF NOT EXISTS sq_ticket_{session.Id} START WITH 1;");

        // Инициализация состояний исполнителей (если есть пользователи с ролью Executor)
        // TODO: Реализовать при добавлении UserRole/Role сущностей

        // Публикация доменного события
        await _eventPublisher.PublishAsync(new QueueSessionCreatedEvent(
            session.Id, configId, createdById));

        return await GetByIdAsync(session.Id);
    }

    /// <summary>
    /// Возвращает сессию по ID
    /// </summary>
    public async Task<QueueSessionDto?> GetByIdAsync(int id)
    {
        var session = await _context.QueueSessions
            .Include(q => q.QueueConfig)
            .Include(q => q.CreatedBy)
            .Include(q => q.Tickets)
            .Include(q => q.ExecutorStates)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (session == null)
            return null;

        return MapToDto(session);
    }

    /// <summary>
    /// Возвращает все сессии. При filterActive=true возвращает только активную сессию (OPEN или PAUSED)
    /// </summary>
    public async Task<List<QueueSessionDto>> GetAllAsync(bool filterActive = false)
    {
        var query = _context.QueueSessions.AsQueryable();

        if (filterActive)
        {
            query = query.Where(q => q.Status == SessionStatus.Open || q.Status == SessionStatus.Paused);
        }

        var sessions = await query
            .Include(q => q.QueueConfig)
            .Include(q => q.CreatedBy)
            .Include(q => q.Tickets)
            .Include(q => q.ExecutorStates)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        return sessions.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Изменение статуса сессии с валидацией переходов
    /// </summary>
    public async Task<QueueSessionDto> ChangeStatusAsync(
        int sessionId,
        SessionStatus newStatus,
        int actorUserId)
    {
        var session = await _context.QueueSessions
            .Include(q => q.QueueConfig)
            .Include(q => q.CreatedBy)
            .Include(q => q.Tickets)
            .Include(q => q.ExecutorStates)
            .FirstOrDefaultAsync(q => q.Id == sessionId);

        if (session == null)
        {
            throw new NotFoundException($"QueueSession with id {sessionId} not found");
        }

        var oldStatus = session.Status;
        
        // Валидация переходов
        ValidateStatusTransition(oldStatus, newStatus);

        // DRAFT -> OPEN или PAUSED -> OPEN: нельзя если есть активные сессии в системе
        if ((oldStatus == SessionStatus.Draft || oldStatus == SessionStatus.Paused) && newStatus == SessionStatus.Open)
        {
            var activeSession = await _context.QueueSessions
                .AnyAsync(q => (q.Status == SessionStatus.Open || q.Status == SessionStatus.Paused) && q.Id != sessionId);

            if (activeSession)
            {
                throw new BadRequestException("There is already an active session in the system");
            }
        }

        // При переходе в OPEN: установка StartedAt
        if (newStatus == SessionStatus.Open)
        {
            session.StartedAt = DateTime.UtcNow;
        }

        // При переходе в CLOSED
        if (newStatus == SessionStatus.Closed)
        {
            session.ClosedAt = DateTime.UtcNow;

            // Удаление последовательности
            await _context.Database.ExecuteSqlRawAsync(
                $"DROP SEQUENCE IF EXISTS sq_ticket_{sessionId}");

            // Закрытие талонов и инвалидация клиентских сессий выполняются в эндпоинте
            // WAITING -> CANCELLED, CALLED -> SKIPPED, SERVING -> SERVED
        }

        // Установка нового статуса
        session.Status = newStatus;
        await _context.SaveChangesAsync();

        // Публикация доменного события
        await _eventPublisher.PublishAsync(new QueueSessionStatusChangedEvent(
            session.Id, newStatus, oldStatus, actorUserId));

        return MapToDto(session);
    }

    /// <summary>
            /// Агрегация метрик по талонам и исполнителям
            /// </summary>
            public async Task<QueueSessionStatsDto> GetStatisticsAsync(int sessionId)
            {
                var session = await _context.QueueSessions
                    .Include(q => q.Tickets)
                    .FirstOrDefaultAsync(q => q.Id == sessionId);
    
                if (session == null)
                {
                    throw new NotFoundException($"QueueSession with id {sessionId} not found");
                }
    
                return await GetStatisticsForSessionAsync(session);
            }
    
            /// <summary>
            /// Возвращает статистику для активной сессии
            /// </summary>
            public async Task<QueueSessionStatsDto> GetStatisticsAsync()
            {
                var session = await GetActiveSessionAsync();
                if (session == null)
                {
                    throw new NotFoundException("No active queue session found");
                }
    
                return await GetStatisticsForSessionAsync(session);
            }
    
            /// <summary>
            /// Вспомогательный метод для агрегации статистики по сессии
            /// </summary>
            private async Task<QueueSessionStatsDto> GetStatisticsForSessionAsync(QueueSession session)
            {
                var tickets = session.Tickets;
    
                var totalTickets = tickets.Count;
                var waitingTickets = tickets.Count(t => t.Status == TicketStatus.Waiting);
                var calledTickets = tickets.Count(t => t.Status == TicketStatus.Called);
                var servingTickets = tickets.Count(t => t.Status == TicketStatus.Serving);
                var servedTickets = tickets.Count(t => t.Status == TicketStatus.Served);
                var skippedTickets = tickets.Count(t => t.Status == TicketStatus.Skipped);
                var cancelledTickets = tickets.Count(t => t.Status == TicketStatus.Cancelled);
    
                // Среднее время обслуживания
                double? avgServiceTime = null;
                var servedTicketsWithTimes = tickets
                    .Where(t => t.Status == TicketStatus.Served &&
                               t.ServiceStartedAt.HasValue &&
                               t.ServiceEndedAt.HasValue)
                    .ToList();
    
                if (servedTicketsWithTimes.Count > 0)
                {
                    var totalSeconds = servedTicketsWithTimes.Sum(t =>
                        (t.ServiceEndedAt.Value - t.ServiceStartedAt.Value!).TotalSeconds);
                    avgServiceTime = totalSeconds / servedTicketsWithTimes.Count;
                }
    
                // Длительность сессии
                TimeSpan? sessionDuration = null;
                if (session.StartedAt.HasValue)
                {
                    var endTime = session.ClosedAt ?? DateTime.UtcNow;
                    sessionDuration = endTime - session.StartedAt.Value;
                }
    
                return new QueueSessionStatsDto(
                    totalTickets,
                    waitingTickets,
                    calledTickets,
                    servingTickets,
                    servedTickets,
                    skippedTickets,
                    cancelledTickets,
                    avgServiceTime,
                    sessionDuration
                );
            }

    /// <summary>
    /// Расчёт времени ожидания для талона согласно логической модели данных (раздел 4.7)
    /// время_ожидания = (людей_передо_мной × среднее_время_обслуживания) / активных_исполнителей
    /// </summary>
    public async Task<int?> CalculateEstimatedWaitTimeAsync(int ticketId)
    {
        // 1. Получаем талон с сессией
        var ticket = await _context.Tickets
            .Include(t => t.QueueSession)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
            return null;

        var queueSessionId = ticket.QueueSessionId;

        // 2. people_before_me = COUNT(WAITING с sort_order < текущей)
        var peopleBeforeMe = await _context.Tickets
            .Where(t => t.QueueSessionId == queueSessionId
                        && t.Status == TicketStatus.Waiting
                        && t.SortOrder < ticket.SortOrder)
            .CountAsync();

        // 3. Среднее время обслуживания = AVG по SERVED талонам в секундах
        double? avgServiceTimeSeconds = null;
        var servedTickets = await _context.Tickets
            .Where(t => t.QueueSessionId == queueSessionId
                        && t.Status == TicketStatus.Served
                        && t.ServiceStartedAt.HasValue
                        && t.ServiceEndedAt.HasValue)
            .ToListAsync();

        if (servedTickets.Count > 0)
        {
            var totalSeconds = servedTickets.Sum(t =>
                (t.ServiceEndedAt.Value - t.ServiceStartedAt.Value!).TotalSeconds);
            avgServiceTimeSeconds = totalSeconds / servedTickets.Count;
        }

        // Для MVP: если нет завершённых талонов, используем плановое среднее время
        // Но только если у талона есть привязка к типу услуги
        if ((avgServiceTimeSeconds == null || avgServiceTimeSeconds == 0) && ticket.ServiceTypeId.HasValue)
        {
            // Берём PlanAvgServiceTimeSec из конкретного типа услуги талона
            var serviceType = await _context.ServiceTypes
                .FirstOrDefaultAsync(st => st.Id == ticket.ServiceTypeId.Value);

            if (serviceType?.PlanAvgServiceTimeSec.HasValue == true)
            {
                avgServiceTimeSeconds = serviceType.PlanAvgServiceTimeSec.Value;
            }
        }

        // 4. Активные исполнители = COUNT(is_ready = true) + COUNT(current_ticket_id IS NOT NULL)
        var activeExecutors = await _context.ExecutorStates
            .Where(es => es.QueueSessionId == queueSessionId
                        && (es.IsReady || es.CurrentTicketId.HasValue))
            .CountAsync();

        // 5. Расчёт времени ожидания
                // Формула: (людей_передо_мной + 1) × среднее_время_обслуживания / активных_исполнителей
                if (avgServiceTimeSeconds == null)
                    return null; // Нет данных для расчёта
        
                double waitTimeMinutes;
                if (activeExecutors == 0)
                {
                    // Если активных исполнителей = 0, показываем время для одного исполнителя
                    waitTimeMinutes = ((peopleBeforeMe + 1) * avgServiceTimeSeconds.Value) / 60.0;
                }
                else
                {
                    waitTimeMinutes = ((peopleBeforeMe + 1) * avgServiceTimeSeconds.Value) / activeExecutors / 60.0;
                }

        return (int)Math.Round(waitTimeMinutes);
    }

    /// <summary>
    /// Валидация переходов статусов
    /// </summary>
    private void ValidateStatusTransition(SessionStatus current, SessionStatus next)
    {
        var validTransitions = new Dictionary<SessionStatus, HashSet<SessionStatus>>
        {
            [SessionStatus.Draft] = new HashSet<SessionStatus> { SessionStatus.Open, SessionStatus.Closed },
            [SessionStatus.Open] = new HashSet<SessionStatus> { SessionStatus.Paused, SessionStatus.Closed },
            [SessionStatus.Paused] = new HashSet<SessionStatus> { SessionStatus.Open, SessionStatus.Closed },
            [SessionStatus.Closed] = new HashSet<SessionStatus> { SessionStatus.Draft }
        };

        if (!validTransitions.TryGetValue(current, out var allowed))
        {
            throw new BadRequestException($"No valid transitions from status {current}");
        }

        if (!allowed.Contains(next))
        {
            throw new BadRequestException(
                $"Invalid status transition from {current} to {next}");
        }
    }

    /// <summary>
    /// Преобразование в QueueSessionDto
    /// </summary>
    private QueueSessionDto MapToDto(QueueSession session)
    {
        return new QueueSessionDto(
            session.Id,
            session.QueueConfigId,
            session.QueueConfig?.Name ?? string.Empty,
            session.Status,
            session.StartedAt,
            session.ClosedAt,
            session.CreatedById,
            session.CreatedBy?.FullName,
            session.CreatedAt
        );
    }
}
