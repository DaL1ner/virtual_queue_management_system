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
/// Сервис для управления талонами (записями в очереди)
/// </summary>
public class TicketService
{
    private readonly AppDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly QueueSessionService _queueSessionService;
    private readonly ILogger<TicketService> _logger;

    public TicketService(AppDbContext context, IEventPublisher eventPublisher, QueueSessionService queueSessionService, ILogger<TicketService> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _queueSessionService = queueSessionService;
        _logger = logger;
    }

    /// <summary>
    /// Создание нового талона
    /// </summary>
    public async Task<TicketDto> CreateAsync(CreateTicketDto dto, int? clientSessionId = null, int? actorUserId = null)
    {
        // 1. Получение активной сессии очереди (в системе может быть только одна)
        var session = await _queueSessionService.GetActiveSessionAsync();
        if (session == null)
            throw new BadRequestException("Нет активной сессии очереди.");

        // 2. Определение типа услуги и приоритета
        // Если типы услуг разрешены в конфигурации очереди, используем переданный ServiceTypeId
        // Иначе принудительно устанавливаем null
        ServiceType? serviceType = null;
        if (session.QueueConfig.IsServiceTypeEnabled && dto.ServiceTypeId.HasValue)
        {
            serviceType = await _context.ServiceTypes
                .FirstOrDefaultAsync(st => st.Id == dto.ServiceTypeId && st.QueueConfigId == session.QueueConfigId && st.IsActive);
            if (serviceType == null)
                throw new BadRequestException($"Тип услуги с ID {dto.ServiceTypeId} не найден или не активен.");
        }
        else if (session.QueueConfig.IsServiceTypeEnabled && !dto.ServiceTypeId.HasValue)
        {
            // Если выбор услуги включён, но не указан - используем базовую услугу (приоритет 0, буква 'A')
            serviceType = await _context.ServiceTypes
                .FirstOrDefaultAsync(st => st.QueueConfigId == session.QueueConfigId && st.BasePriorityLevel == 0 && st.IsActive);
            if (serviceType == null)
                throw new BadRequestException("Для данной очереди требуется выбор услуги, но базовая услуга не настроена.");
        }
        // Если IsServiceTypeEnabled = false, serviceType остаётся null

        // 3. Аннулирование предыдущих активных талонов для этой клиентской сессии (если указана)
        if (clientSessionId.HasValue)
        {
            var activeTickets = await _context.Tickets
                .Where(t => t.ClientSessionId == clientSessionId &&
                           t.QueueSessionId == session.Id &&
                           (t.Status == TicketStatus.Waiting || t.Status == TicketStatus.Called))
                .ToListAsync();
            foreach (var activeTicket in activeTickets)
            {
                activeTicket.Status = TicketStatus.Cancelled;
                activeTicket.ServiceEndedAt = DateTime.UtcNow;
                activeTicket.CancelReason = "Автоматическая отмена при создании нового талона";
                activeTicket.Version++;
                await _eventPublisher.PublishAsync(new TicketCancelledEvent(activeTicket.Id, activeTicket.QueueSessionId, actorUserId));
            }
            if (activeTickets.Any())
                await _context.SaveChangesAsync();
        }

        // 4. Получение следующего номера из последовательности сессии
        var nextNumber = await GetNextTicketNumberAsync(session.Id, serviceType?.Letter ?? 'A');

        // 5. Вычисление sort_order
        var maxSortOrder = await _context.Tickets
            .Where(t => t.QueueSessionId == session.Id)
            .MaxAsync(t => (decimal?)t.SortOrder) ?? 0;
        var newSortOrder = maxSortOrder + 1000;

        // 6. Создание талона
        var ticket = new Ticket
        {
            QueueSessionId = session.Id,
            ServiceTypeId = serviceType?.Id,
            TicketNumber = nextNumber,
            ClientName = dto.ClientName,
            ClientSurname = dto.ClientSurname,
            SortOrder = newSortOrder,
            PriorityLevel = serviceType?.BasePriorityLevel ?? 0,
            Status = TicketStatus.Waiting,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            ClientSessionId = clientSessionId,
        };

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        // 7. Публикация события
        await _eventPublisher.PublishAsync(new TicketCreatedEvent(ticket.Id, ticket.QueueSessionId, clientSessionId ?? 0));

        // 8. Возврат DTO
        return await MapToDtoAsync(ticket);
    }

    /// <summary>
    /// Получение следующего номера талона (буква + номер из последовательности)
    /// </summary>
    private async Task<string> GetNextTicketNumberAsync(int sessionId, char serviceLetter)
    {
        // Используем последовательность sq_ticket_{sessionId}
        var sequenceName = $"sq_ticket_{sessionId}";
        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT nextval('{sequenceName}')";
        var result = await command.ExecuteScalarAsync();
        long nextNum = Convert.ToInt64(result);
        return $"{serviceLetter}-{nextNum:D3}";
    }

    /// <summary>
    /// Изменение статуса талона
    /// </summary>
    public async Task<TicketDto> ChangeStatusAsync(int ticketId, TicketStatus newStatus, int? actorUserId = null, int? executorUserId = null)
    {
        var ticket = await _context.Tickets
            .Include(t => t.ServiceType)
            .Include(t => t.ServedByUser)
            .FirstOrDefaultAsync(t => t.Id == ticketId);
        if (ticket == null)
            throw new NotFoundException($"Талон с ID {ticketId} не найден.");

        var oldStatus = ticket.Status;

        // Валидация перехода
        ValidateStatusTransition(oldStatus, newStatus);

        // Обновление полей в зависимости от статуса
        switch (newStatus)
        {
            case TicketStatus.Called:
                ticket.CalledAt = DateTime.UtcNow;
                break;
            case TicketStatus.Serving:
                ticket.ServiceStartedAt = DateTime.UtcNow;
                break;
            case TicketStatus.Served:
            case TicketStatus.Skipped:
            case TicketStatus.Cancelled:
                ticket.ServiceEndedAt = DateTime.UtcNow;
                if (executorUserId.HasValue)
                    ticket.ServedByUserId = executorUserId;
                break;
        }

        ticket.Status = newStatus;
        ticket.Version++;

        await _context.SaveChangesAsync();

        // Публикация события
        await _eventPublisher.PublishAsync(new TicketStatusChangedEvent(ticket.Id, ticket.QueueSessionId, newStatus, oldStatus, actorUserId));

        // Если статус Called, дополнительно публикуем TicketCalledEvent
        if (newStatus == TicketStatus.Called)
            await _eventPublisher.PublishAsync(new TicketCalledEvent(ticket.Id, ticket.QueueSessionId, executorUserId));

        return await MapToDtoAsync(ticket);
    }

    /// <summary>
    /// Перемещение талона на N шагов назад (дальше от начала очереди)
    /// </summary>
    public async Task<TicketDto> MoveBackwardAsync(int ticketId, int steps, int? actorUserId = null)
    {
        if (steps <= 0)
            throw new BadRequestException("Количество шагов должно быть положительным числом.");

        // Загрузка талона
        var ticket = await _context.Tickets
            .Include(t => t.ServiceType)
            .Include(t => t.ServedByUser)
            .FirstOrDefaultAsync(t => t.Id == ticketId);
        if (ticket == null)
            throw new NotFoundException($"Талон с ID {ticketId} не найден.");

        // Проверка статуса
        if (ticket.Status != TicketStatus.Waiting)
            throw new BadRequestException("Перемещать можно только талоны в статусе WAITING.");

        // Получение всех ожидающих талонов в сессии, отсортированных по приоритету и sort_order
        var waitingTickets = await _context.Tickets
            .Where(t => t.QueueSessionId == ticket.QueueSessionId && t.Status == TicketStatus.Waiting)
            .OrderByDescending(t => t.PriorityLevel)
            .ThenBy(t => t.SortOrder)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync();

        // Находим индекс текущего талона в отсортированном списке
        int currentIndex = waitingTickets.FindIndex(t => t.Id == ticketId);
        if (currentIndex < 0)
            throw new BadRequestException("Талон не найден среди ожидающих талонов сессии.");

        // Вычисляем целевую позицию (индекс)
        int targetIndex = currentIndex + steps;
        if (targetIndex >= waitingTickets.Count)
            targetIndex = waitingTickets.Count - 1; // перемещаем в конец очереди

        // Если целевая позиция равна текущей, ничего не делаем
        if (targetIndex == currentIndex)
            return await MapToDtoAsync(ticket);

        // Логирование для отладки
        _logger.LogInformation("MoveBackwardAsync: ticketId={TicketId}, currentIndex={CurrentIndex}, targetIndex={TargetIndex}, waitingTicketsCount={Count}",
            ticketId, currentIndex, targetIndex, waitingTickets.Count);
        _logger.LogInformation("Current ticket sort_order={SortOrder}, priority={Priority}", ticket.SortOrder, ticket.PriorityLevel);
        for (int i = 0; i < waitingTickets.Count; i++)
        {
            _logger.LogInformation("  [{Index}] Id={Id}, sort_order={SortOrder}, priority={Priority}",
                i, waitingTickets[i].Id, waitingTickets[i].SortOrder, waitingTickets[i].PriorityLevel);
        }

        // Исключаем перемещаемый талон из списка для определения соседей
        var otherTickets = waitingTickets.Where(t => t.Id != ticketId).ToList();
        
        // Определяем соседние талоны вокруг целевой позиции, исключая текущий талон
        Ticket? prevTicket = null;
        Ticket? nextTicket = null;
        
        if (otherTickets.Count == 0)
        {
            // В очереди только один талон (перемещаемый)
            prevTicket = null;
            nextTicket = null;
        }
        else if (targetIndex == 0)
        {
            // Перемещаем в начало очереди (перед первым талоном)
            prevTicket = null;
            nextTicket = otherTickets[0];
        }
        else if (targetIndex >= otherTickets.Count)
        {
            // Перемещаем в конец очереди (после последнего талона)
            prevTicket = otherTickets[otherTickets.Count - 1];
            nextTicket = null;
        }
        else
        {
            // Перемещаем между талонами
            // targetIndex указывает на позицию в исходном списке (включая текущий талон)
            // После исключения текущего талона, индексы смещаются:
            // Если currentIndex < targetIndex, то targetIndex в otherTickets уменьшается на 1
            // Но нам нужен индекс следующего талона после вставки, поэтому используем targetIndex как есть
            // (потому что otherTickets уже не содержит текущий талон)
            int insertPos = targetIndex; // позиция, после которой будет вставлен талон
            // prevTicket = элемент перед insertPos, nextTicket = элемент на позиции insertPos
            prevTicket = otherTickets[insertPos - 1];
            nextTicket = otherTickets[insertPos];
        }

        _logger.LogInformation("Neighbors: prevTicket={PrevId}(sort={PrevSort}), nextTicket={NextId}(sort={NextSort})",
            prevTicket?.Id, prevTicket?.SortOrder, nextTicket?.Id, nextTicket?.SortOrder);

        // Вычисляем новый sort_order
        decimal newSortOrder;
        if (prevTicket == null && nextTicket == null)
        {
            // В очереди только один талон
            newSortOrder = ticket.SortOrder;
        }
        else if (prevTicket == null)
        {
            // Перемещаем в начало очереди (но мы двигаем назад, так что этот случай маловероятен)
            // Используем минимальный sort_order среди всех талонов, уменьшенный на 1000, чтобы гарантировать уникальность
            decimal minSortOrder = otherTickets.Min(t => t.SortOrder);
            newSortOrder = minSortOrder - 1000;
            // Убедимся, что newSortOrder не отрицательный (ограничение CHECK sort_order >= 0)
            if (newSortOrder < 0)
                newSortOrder = 0;
        }
        else if (nextTicket == null)
        {
            // Перемещаем в конец очереди
            // Используем максимальный sort_order среди всех талонов, увеличенный на 1000, чтобы гарантировать уникальность
            decimal maxSortOrder = otherTickets.Max(t => t.SortOrder);
            newSortOrder = maxSortOrder + 1000;
        }
        else
        {
            // Среднее арифметическое между sort_order соседних талонов
            newSortOrder = (prevTicket.SortOrder + nextTicket.SortOrder) / 2;
        }

        _logger.LogInformation("Calculated newSortOrder={NewSortOrder}", newSortOrder);

        // Проверяем, не стал ли newSortOrder равен одному из соседних (из-за ограничений точности)
        // Если разница меньше минимального шага (0.001), корректируем
        decimal minStep = 0.001m;
        if (prevTicket != null && Math.Abs(newSortOrder - prevTicket.SortOrder) < minStep)
        {
            _logger.LogInformation("Adjusting newSortOrder too close to prevTicket");
            newSortOrder = prevTicket.SortOrder + minStep;
        }
        if (nextTicket != null && Math.Abs(newSortOrder - nextTicket.SortOrder) < minStep)
        {
            _logger.LogInformation("Adjusting newSortOrder too close to nextTicket");
            newSortOrder = nextTicket.SortOrder - minStep;
        }

        // Дополнительная проверка: убедимся, что новый sort_order не совпадает с существующим у другого талона
        // (кроме самого перемещаемого талона)
        bool duplicateExists = otherTickets.Any(t => Math.Abs(t.SortOrder - newSortOrder) < minStep);
        if (duplicateExists)
        {
            _logger.LogWarning("Duplicate sort_order detected, adjusting");
            // Если обнаружен дубликат, сдвигаем на минимальный шаг в сторону от соседа
            if (prevTicket != null)
                newSortOrder = prevTicket.SortOrder + minStep;
            else if (nextTicket != null)
                newSortOrder = nextTicket.SortOrder - minStep;
            else
                newSortOrder += minStep;
        }

        // Определяем, изменился ли приоритет (приоритет целевой позиции)
        // Берем приоритет талона, который будет на целевой позиции после перемещения
        int targetPriority;
        if (otherTickets.Count == 0)
        {
            targetPriority = ticket.PriorityLevel;
        }
        else if (targetIndex == 0)
        {
            // Перемещаем в начало, берем приоритет первого талона (который будет после перемещаемого)
            targetPriority = otherTickets[0].PriorityLevel;
        }
        else if (targetIndex >= otherTickets.Count)
        {
            // Если перемещаем в конец, берем приоритет последнего талона
            targetPriority = otherTickets[otherTickets.Count - 1].PriorityLevel;
        }
        else
        {
            // Берем приоритет талона, который будет на позиции targetIndex после перемещения
            // После исключения текущего талона, элемент на позиции targetIndex смещается на -1
            int adjustedIndex = targetIndex - 1;
            targetPriority = otherTickets[adjustedIndex].PriorityLevel;
        }
        
        bool priorityChanged = ticket.PriorityLevel != targetPriority;
        int oldPriority = ticket.PriorityLevel;

        // Сохраняем старую позицию для события
        int oldPosition = currentIndex + 1;
        int newPosition = targetIndex + 1;

        _logger.LogInformation("Updating ticket: newSortOrder={NewSortOrder}, targetPriority={TargetPriority}, priorityChanged={PriorityChanged}",
            newSortOrder, targetPriority, priorityChanged);

        // Обновляем талон
        ticket.SortOrder = newSortOrder;
        if (priorityChanged)
            ticket.PriorityLevel = targetPriority;
        ticket.Version++;

        // Сохраняем изменения
        await _context.SaveChangesAsync();

        // Публикация событий
        await _eventPublisher.PublishAsync(new TicketMovedEvent(ticket.Id, ticket.QueueSessionId, oldPosition, newPosition, actorUserId));
        if (priorityChanged)
            await _eventPublisher.PublishAsync(new PriorityChangedEvent(ticket.Id, ticket.QueueSessionId, oldPriority, targetPriority, actorUserId));

        _logger.LogInformation("Ticket moved successfully from position {OldPosition} to {NewPosition}", oldPosition, newPosition);

        return await MapToDtoAsync(ticket);
    }

    /// <summary>
    /// Получение позиции талона в очереди
    /// </summary>
    public async Task<TicketPositionDto> GetPositionAsync(int ticketId)
    {
        var ticket = await _context.Tickets
            .Include(t => t.ServiceType)
            .Include(t => t.ServedByUser)
            .FirstOrDefaultAsync(t => t.Id == ticketId);
        if (ticket == null)
            throw new NotFoundException($"Талон с ID {ticketId} не найден.");

        // Вычисляем позицию
        int position = await CalculatePositionAsync(ticket);

        // Общее количество ожидающих талонов в сессии
        int totalWaiting = await _context.Tickets
            .CountAsync(t => t.QueueSessionId == ticket.QueueSessionId && t.Status == TicketStatus.Waiting);

        // Оценочное время ожидания (опционально, можно рассчитать позже)
        int? estimatedWaitMinutes = null;

        return new TicketPositionDto(ticket.Id, position, totalWaiting, estimatedWaitMinutes);
    }

    /// <summary>
    /// Валидация переходов статусов
    /// </summary>
    private void ValidateStatusTransition(TicketStatus current, TicketStatus next)
    {
        var validTransitions = new Dictionary<TicketStatus, HashSet<TicketStatus>>
        {
            [TicketStatus.Waiting] = new HashSet<TicketStatus> { TicketStatus.Called, TicketStatus.Cancelled },
            [TicketStatus.Called] = new HashSet<TicketStatus> { TicketStatus.Serving, TicketStatus.Skipped },
            [TicketStatus.Serving] = new HashSet<TicketStatus> { TicketStatus.Served },
            [TicketStatus.Served] = new HashSet<TicketStatus> { },
            [TicketStatus.Skipped] = new HashSet<TicketStatus> { },
            [TicketStatus.Cancelled] = new HashSet<TicketStatus> { },
        };

        if (!validTransitions.TryGetValue(current, out var allowed))
            throw new BadRequestException($"Недопустимый переход из статуса {current}");

        if (!allowed.Contains(next))
            throw new BadRequestException($"Переход из {current} в {next} запрещён.");
    }

    /// <summary>
    /// Получение списка всех талонов активной сессии (без сортировки)
    /// </summary>
    public async Task<IEnumerable<TicketDto>> GetAllBySessionAsync(bool includeSorted = false)
    {
        var session = await _queueSessionService.GetActiveSessionAsync();
        if (session == null)
            throw new BadRequestException("Нет активной сессии очереди.");

        var query = _context.Tickets
            .Include(t => t.ServiceType)
            .Include(t => t.ServedByUser)
            .Where(t => t.QueueSessionId == session.Id);

        if (includeSorted)
        {
            query = query.OrderByDescending(t => t.PriorityLevel)
                         .ThenBy(t => t.SortOrder)
                         .ThenBy(t => t.CreatedAt);
        }

        var tickets = await query.ToListAsync();
        var dtos = new List<TicketDto>();
        foreach (var ticket in tickets)
        {
            dtos.Add(await MapToDtoAsync(ticket));
        }
        return dtos;
    }

    /// <summary>
    /// Получение конкретного талона по ID
    /// </summary>
    public async Task<TicketDto?> GetByIdAsync(int ticketId)
    {
        var ticket = await _context.Tickets
            .Include(t => t.ServiceType)
            .Include(t => t.ServedByUser)
            .Include(t => t.QueueSession)
            .FirstOrDefaultAsync(t => t.Id == ticketId);
        if (ticket == null)
            return null;

        return await MapToDtoAsync(ticket);
    }

    /// <summary>
    /// Преобразование Ticket в TicketDto
    /// </summary>
    private async Task<TicketDto> MapToDtoAsync(Ticket ticket)
    {
        // Вычисление позиции в очереди
        var position = await CalculatePositionAsync(ticket);

        return new TicketDto(
            ticket.Id,
            ticket.QueueSessionId,
            ticket.TicketNumber,
            ticket.ClientName,
            ticket.ClientSurname,
            ticket.ServiceTypeId,
            ticket.ServiceType?.Name,
            ticket.ServiceType?.Letter,
            (int)ticket.SortOrder,
            ticket.PriorityLevel,
            ticket.Status,
            ticket.Version,
            ticket.CreatedAt,
            ticket.CalledAt,
            ticket.ServiceStartedAt,
            ticket.ServiceEndedAt,
            ticket.ServedByUserId,
            ticket.ServedByUser?.FullName,
            ticket.CancelReason,
            position
        );
    }

    /// <summary>
    /// Вычисление позиции талона в очереди (количество WAITING талонов с более высоким приоритетом/позицией)
    /// </summary>
    private async Task<int> CalculatePositionAsync(Ticket ticket)
    {
        if (ticket.Status != TicketStatus.Waiting)
            return 0;

        var matchingTickets = await _context.Tickets
            .Where(t => t.QueueSessionId == ticket.QueueSessionId &&
                       t.Status == TicketStatus.Waiting &&
                       t.Id != ticket.Id &&
                       (t.PriorityLevel > ticket.PriorityLevel ||
                        (t.PriorityLevel == ticket.PriorityLevel && t.SortOrder < ticket.SortOrder) ||
                        (t.PriorityLevel == ticket.PriorityLevel && t.SortOrder == ticket.SortOrder && t.CreatedAt < ticket.CreatedAt)))
            .ToListAsync();

        var count = matchingTickets.Count;
        var position = count + 1;

        return position;
    }

    /// <summary>
    /// Получение активного талона по client_session_id
    /// Активный талон - это талон со статусом WAITING, CALLED или SERVING
    /// </summary>
    public async Task<Ticket?> GetActiveTicketByClientSessionIdAsync(int clientSessionId)
    {
        return await _context.Tickets
            .Where(t => t.ClientSessionId == clientSessionId &&
                       (t.Status == TicketStatus.Waiting ||
                        t.Status == TicketStatus.Called ||
                        t.Status == TicketStatus.Serving))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Получение всех активных талонов по client_session_id
    /// </summary>
    public async Task<List<Ticket>> GetActiveTicketsByClientSessionIdAsync(int clientSessionId)
    {
        return await _context.Tickets
            .Where(t => t.ClientSessionId == clientSessionId &&
                       (t.Status == TicketStatus.Waiting ||
                        t.Status == TicketStatus.Called))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Аннулирование всех активных талонов по client_session_id
    /// </summary>
    public async Task<int> InvalidateTicketsByClientSessionIdAsync(
        int clientSessionId,
        string reason,
        int? actorUserId = null)
    {
        var activeTickets = await GetActiveTicketsByClientSessionIdAsync(clientSessionId);
        
        if (activeTickets.Count == 0)
            return 0;

        foreach (var ticket in activeTickets)
        {
            ticket.Status = TicketStatus.Cancelled;
            ticket.ServiceEndedAt = DateTime.UtcNow;
            ticket.CancelReason = reason;
            ticket.Version++;
            await _eventPublisher.PublishAsync(new TicketCancelledEvent(ticket.Id, ticket.QueueSessionId, actorUserId));
        }

        await _context.SaveChangesAsync();
        return activeTickets.Count;
    }

    /// <summary>
    /// Аннулирование предыдущих активных талонов для client_session_id в конкретной сессии
    /// Используется при создании нового талона для той же клиентской сессии
    /// </summary>
    public async Task<int> InvalidateTicketsByClientSessionIdAndSessionIdAsync(
        int clientSessionId,
        int queueSessionId,
        string reason,
        int? actorUserId = null)
    {
        var activeTickets = await _context.Tickets
            .Where(t => t.ClientSessionId == clientSessionId &&
                       t.QueueSessionId == queueSessionId &&
                       (t.Status == TicketStatus.Waiting || t.Status == TicketStatus.Called))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
        
        if (activeTickets.Count == 0)
            return 0;

        foreach (var ticket in activeTickets)
        {
            ticket.Status = TicketStatus.Cancelled;
            ticket.ServiceEndedAt = DateTime.UtcNow;
            ticket.CancelReason = reason;
            ticket.Version++;
            await _eventPublisher.PublishAsync(new TicketCancelledEvent(ticket.Id, ticket.QueueSessionId, actorUserId));
        }

        await _context.SaveChangesAsync();
        return activeTickets.Count;
    }

    /// <summary>
    /// Отмена талона (перевод из WAITING в CANCELLED) с завершением клиентской сессии
    /// </summary>
    public async Task<TicketDto> CancelTicketAsync(int ticketId, int? actorUserId = null)
    {
        var ticket = await _context.Tickets
            .Include(t => t.ServiceType)
            .Include(t => t.ServedByUser)
            .Include(t => t.ClientSession)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
            throw new NotFoundException($"Талон с ID {ticketId} не найден.");

        if (ticket.Status != TicketStatus.Waiting)
            throw new BadRequestException("Можно отменить только талон в статусе WAITING.");

        var oldStatus = ticket.Status;
        var clientSessionId = ticket.ClientSessionId;

        // Обновление полей талона
        ticket.Status = TicketStatus.Cancelled;
        ticket.ServiceEndedAt = DateTime.UtcNow;
        ticket.CancelReason = "Выход/исключение до начала обслуживания";
        ticket.Version++;

        await _context.SaveChangesAsync();

        // Публикация события отмены талона
        await _eventPublisher.PublishAsync(new TicketCancelledEvent(ticket.Id, ticket.QueueSessionId, actorUserId));
        await _eventPublisher.PublishAsync(new TicketStatusChangedEvent(ticket.Id, ticket.QueueSessionId, TicketStatus.Cancelled, oldStatus, actorUserId));

        // Завершение клиентской сессии, если она есть
        if (clientSessionId.HasValue)
        {
            await InvalidateClientSessionAsync(clientSessionId.Value, actorUserId);
        }

        return await MapToDtoAsync(ticket);
    }

    /// <summary>
    /// Завершение клиентской сессии и аннулирование связанных талонов
    /// </summary>
    private async Task InvalidateClientSessionAsync(int clientSessionId, int? actorUserId = null)
    {
        var session = await _context.ClientSessions
            .FirstOrDefaultAsync(cs => cs.Id == clientSessionId);

        if (session == null)
            return;

        session.IsActive = false;
        await _context.SaveChangesAsync();

        // Аннулирование всех активных талонов сессии
        await InvalidateTicketsByClientSessionIdAsync(
            clientSessionId,
            "Клиентская сессия завершена при отмене талона",
            actorUserId);

        // Публикация события завершения сессии
        await _eventPublisher.PublishAsync(new ClientSessionInvalidatedEvent(
            clientSessionId,
            null,
            actorUserId ?? 1));
    }

    /// <summary>
    /// Получение списка ожидающих талонов (очереди) в активной сессии
    /// Сортировка: PriorityLevel DESC, SortOrder ASC, CreatedAt ASC
    /// </summary>
    public async Task<IEnumerable<TicketDto>> GetQueueAsync()
    {
        var session = await _queueSessionService.GetActiveSessionAsync();
        if (session == null)
            throw new BadRequestException("Нет активной сессии очереди.");
    
        var tickets = await _context.Tickets
            .Include(t => t.ServiceType)
            .Include(t => t.ServedByUser)
            .Where(t => t.QueueSessionId == session.Id && t.Status == TicketStatus.Waiting)
            .OrderByDescending(t => t.PriorityLevel)
            .ThenBy(t => t.SortOrder)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync();
    
        var dtos = new List<TicketDto>();
        foreach (var ticket in tickets)
        {
            dtos.Add(await MapToDtoAsync(ticket));
        }
        return dtos;
    }
}