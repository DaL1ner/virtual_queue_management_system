namespace Application.Services;

using System.Data.Common;
using Domain.Entities;
using Domain.Enums;
using Application.DTOs;
using Application.Events;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Сервис для управления талонами (записями в очереди)
/// </summary>
public class TicketService
{
    private readonly AppDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly QueueSessionService _queueSessionService;

    public TicketService(AppDbContext context, IEventPublisher eventPublisher, QueueSessionService queueSessionService)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _queueSessionService = queueSessionService;
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
        ServiceType? serviceType = null;
        if (dto.ServiceTypeId.HasValue)
        {
            serviceType = await _context.ServiceTypes
                .FirstOrDefaultAsync(st => st.Id == dto.ServiceTypeId && st.QueueConfigId == session.QueueConfigId && st.IsActive);
            if (serviceType == null)
                throw new BadRequestException($"Тип услуги с ID {dto.ServiceTypeId} не найден или не активен.");
        }
        else if (session.QueueConfig.IsServiceTypeEnabled)
        {
            // Если выбор услуги обязателен, но не указан - используем базовую услугу (приоритет 0, буква 'A')
            serviceType = await _context.ServiceTypes
                .FirstOrDefaultAsync(st => st.QueueConfigId == session.QueueConfigId && st.BasePriorityLevel == 0 && st.IsActive);
            if (serviceType == null)
                throw new BadRequestException("Для данной очереди требуется выбор услуги, но базовая услуга не настроена.");
        }

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

        // Определяем соседние талоны вокруг целевой позиции
        Ticket? prevTicket = targetIndex > 0 ? waitingTickets[targetIndex - 1] : null;
        Ticket? nextTicket = targetIndex < waitingTickets.Count - 1 ? waitingTickets[targetIndex + 1] : null;

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
            newSortOrder = nextTicket!.SortOrder - 1000;
        }
        else if (nextTicket == null)
        {
            // Перемещаем в конец очереди
            newSortOrder = prevTicket.SortOrder + 1000;
        }
        else
        {
            // Среднее арифметическое между sort_order соседних талонов
            newSortOrder = (prevTicket.SortOrder + nextTicket.SortOrder) / 2;
        }

        // Проверяем, не стал ли newSortOrder равен одному из соседних (из-за ограничений точности)
        // Если разница меньше минимального шага (0.001), корректируем
        decimal minStep = 0.001m;
        if (prevTicket != null && Math.Abs(newSortOrder - prevTicket.SortOrder) < minStep)
            newSortOrder = prevTicket.SortOrder + minStep;
        if (nextTicket != null && Math.Abs(newSortOrder - nextTicket.SortOrder) < minStep)
            newSortOrder = nextTicket.SortOrder - minStep;

        // Определяем, изменился ли приоритет (приоритет целевой позиции)
        int targetPriority = waitingTickets[targetIndex].PriorityLevel;
        bool priorityChanged = ticket.PriorityLevel != targetPriority;
        int oldPriority = ticket.PriorityLevel;

        // Сохраняем старую позицию для события
        int oldPosition = currentIndex + 1;
        int newPosition = targetIndex + 1;

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
        return await Task.WhenAll(tickets.Select(MapToDtoAsync));
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

        var count = await _context.Tickets
            .Where(t => t.QueueSessionId == ticket.QueueSessionId &&
                       t.Status == TicketStatus.Waiting &&
                       (t.PriorityLevel > ticket.PriorityLevel ||
                        (t.PriorityLevel == ticket.PriorityLevel && t.SortOrder < ticket.SortOrder) ||
                        (t.PriorityLevel == ticket.PriorityLevel && t.SortOrder == ticket.SortOrder && t.CreatedAt < ticket.CreatedAt)))
            .CountAsync();
        return count + 1;
    }
}