namespace Application.Services;

using Domain.Entities;
using Domain.Enums;
using Application.DTOs;
using Application.Events;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Сервис для управления конфигурациями очередей
/// </summary>
public class QueueConfigService
{
    private readonly AppDbContext _context;
    private readonly IEventPublisher _eventPublisher;

    public QueueConfigService(AppDbContext context, IEventPublisher eventPublisher)
    {
        _context = context;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Возвращает список конфигураций очередей
    /// </summary>
    public async Task<IEnumerable<QueueConfigDto>> GetAllAsync(bool onlyActive = true)
    {
        var query = _context.QueueConfigs
            .Where(q => q.IsActive)
            .Include(q => q.CreatedBy)
            .Include(q => q.ServiceTypes);

        var configs = await query
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        return configs.Select(MapToDto);
    }

    /// <summary>
    /// Возвращает детальную информацию о конфигурации с связанными типами услуг
    /// </summary>
    public async Task<QueueConfigDetailDto> GetByIdAsync(int id)
    {
        var config = await _context.QueueConfigs
            .Include(q => q.CreatedBy)
            .Include(q => q.ServiceTypes)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (config == null)
        {
            throw new NotFoundException($"QueueConfig with id {id} not found");
        }

        var dto = MapToDetailDto(config);
        return dto;
    }

    /// <summary>
    /// Создание новой конфигурации очереди
    /// </summary>
    public async Task<QueueConfigDto> CreateAsync(CreateQueueConfigDto dto, int createdById)
    {
        // Валидация имени
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new BadRequestException("Name is required");
        }

        // Проверка уникальности имени
        var exists = await _context.QueueConfigs
            .AnyAsync(q => q.Name == dto.Name && q.IsActive);

        if (exists)
        {
            throw new ConflictException($"Configuration with name '{dto.Name}' already exists");
        }

        // Валидация PriorityEscalationWaitMin
        if (dto.PriorityEscalationWaitMin.HasValue && dto.PriorityEscalationWaitMin.Value <= 0)
        {
            throw new BadRequestException("PriorityEscalationWaitMin must be greater than 0");
        }

        var config = new QueueConfig
        {
            Name = dto.Name,
            Description = dto.Description,
            DistributionMode = dto.DistributionMode,
            IsServiceTypeEnabled = dto.IsServiceTypeEnabled,
            IsPriorityEnabled = dto.IsPriorityEnabled,
            PriorityEscalationWaitMin = dto.PriorityEscalationWaitMin,
            IsActive = true,
            CreatedById = createdById
        };

        _context.QueueConfigs.Add(config);
        await _context.SaveChangesAsync();

        // Публикация события
        await _eventPublisher.PublishAsync(new QueueConfigCreatedEvent(config.Id, config.Name, createdById));

        return MapToDto(config);
    }

    /// <summary>
    /// Обновление конфигурации
    /// </summary>
    public async Task<QueueConfigDto> UpdateAsync(int id, UpdateQueueConfigDto dto, int actorUserId)
    {
        var config = await _context.QueueConfigs
            .Include(q => q.CreatedBy)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (config == null)
        {
            throw new NotFoundException($"QueueConfig with id {id} not found");
        }

        // Проверка что нет активных сессций при изменении DistributionMode
        if (dto.DistributionMode.HasValue && 
            dto.DistributionMode.Value != config.DistributionMode)
        {
            var activeSession = await _context.QueueSessions
                .AnyAsync(q => q.QueueConfigId == id && q.Status == SessionStatus.Open);

            if (activeSession)
            {
                throw new BadRequestException(
                    "Cannot change DistributionMode while there are active sessions");
            }
        }

        // Обновление полей
        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            config.Name = dto.Name;
        }

        if (dto.Description != null)
        {
            config.Description = dto.Description;
        }

        if (dto.DistributionMode.HasValue)
        {
            config.DistributionMode = dto.DistributionMode.Value;
        }

        if (dto.IsServiceTypeEnabled.HasValue)
        {
            config.IsServiceTypeEnabled = dto.IsServiceTypeEnabled.Value;
        }

        if (dto.IsPriorityEnabled.HasValue)
        {
            config.IsPriorityEnabled = dto.IsPriorityEnabled.Value;
        }

        if (dto.PriorityEscalationWaitMin.HasValue)
        {
            config.PriorityEscalationWaitMin = dto.PriorityEscalationWaitMin.Value;
        }

        await _context.SaveChangesAsync();

        // Публикация события
        await _eventPublisher.PublishAsync(new QueueConfigUpdatedEvent(config.Id, actorUserId));

        return MapToDto(config);
    }

    /// <summary>
    /// Преобразование в QueueConfigDto
    /// </summary>
    private QueueConfigDto MapToDto(QueueConfig config)
    {
        return new QueueConfigDto(
            config.Id,
            config.Name,
            config.Description,
            config.DistributionMode,
            config.IsServiceTypeEnabled,
            config.IsPriorityEnabled,
            config.PriorityEscalationWaitMin,
            config.IsActive,
            config.CreatedById,
            config.CreatedBy?.FullName,
            config.CreatedAt
        );
    }

    /// <summary>
    /// Преобразование в QueueConfigDetailDto
    /// </summary>
    private QueueConfigDetailDto MapToDetailDto(QueueConfig config)
    {
        var configDto = MapToDto(config);
        var serviceTypes = config.ServiceTypes
            .OrderBy(s => s.SortOrder)
            .Select(MapToServiceTypeDto);

        return new QueueConfigDetailDto(configDto, serviceTypes);
    }

    /// <summary>
    /// Преобразование в ServiceTypeDto
    /// </summary>
    private ServiceTypeDto MapToServiceTypeDto(ServiceType serviceType)
    {
        return new ServiceTypeDto(
            serviceType.Id,
            serviceType.QueueConfigId,
            serviceType.Name,
            serviceType.Code,
            serviceType.Letter,
            serviceType.BasePriorityLevel,
            serviceType.PlanAvgServiceTimeSec,
            serviceType.IsActive,
            serviceType.IsHighlighting,
            serviceType.SortOrder,
            serviceType.CreatedAt
        );
    }
}

/// <summary>
/// Исключение - объект не найден
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>
/// Исключение - плохой запрос
/// </summary>
public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
}

/// <summary>
/// Исключение - конфликт
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>
/// Исключение - неавторизованный доступ
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}
