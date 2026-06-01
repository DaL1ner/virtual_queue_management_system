using Application.DTOs;
using Application.Events;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

/// <summary>
/// Сервис для управления конфигурациями очередей.
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
    /// Получает все конфигурации очередей.
    /// </summary>
    public async Task<IEnumerable<QueueConfigDto>> GetAllAsync(bool onlyActive = true)
    {
        var query = _context.QueueConfigs.AsQueryable();

        if (onlyActive)
        {
            query = query.Where(q => q.IsActive);
        }

        var configs = await query.ToListAsync();

        return configs.Select(MapToDto);
    }

    /// <summary>
    /// Получает конфигурацию очереди по ID с детализацией.
    /// </summary>
    public async Task<QueueConfigDetailDto?> GetByIdAsync(int id)
    {
        var config = await _context.QueueConfigs
            .Include(q => q.ServiceTypes)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (config == null)
            return null;

        return MapToDetailDto(config);
    }

    /// <summary>
    /// Создаёт новую конфигурацию очереди.
    /// </summary>
    public async Task<QueueConfigDto> CreateAsync(CreateQueueConfigDto dto, int createdById)
    {
        var config = new QueueConfig
        {
            Name = dto.Name,
            Description = dto.Description,
            DistributionMode = dto.DistributionMode,
            IsServiceTypeEnabled = dto.IsServiceTypeEnabled,
            IsPriorityEnabled = dto.IsPriorityEnabled,
            PriorityEscalationWaitMin = dto.PriorityEscalationWaitMin,
            CreatedById = createdById,
            IsActive = true
        };

        _context.QueueConfigs.Add(config);
        await _context.SaveChangesAsync();

        await _eventPublisher.PublishAsync(new QueueConfigCreatedEvent(config.Id, config.Name, createdById));

        return MapToDto(config);
    }

    /// <summary>
    /// Обновляет существующую конфигурацию очереди.
    /// </summary>
    public async Task<QueueConfigDto> UpdateAsync(int id, UpdateQueueConfigDto dto, int actorUserId)
    {
        var config = await _context.QueueConfigs.FirstOrDefaultAsync(q => q.Id == id);

        if (config == null)
            throw new NotFoundException($"QueueConfig with id {id} not found");

        config.Name = dto.Name ?? config.Name;
        config.Description = dto.Description ?? config.Description;
        config.DistributionMode = dto.DistributionMode ?? config.DistributionMode;
        config.IsServiceTypeEnabled = dto.IsServiceTypeEnabled ?? config.IsServiceTypeEnabled;
        config.IsPriorityEnabled = dto.IsPriorityEnabled ?? config.IsPriorityEnabled;
        config.PriorityEscalationWaitMin = dto.PriorityEscalationWaitMin ?? config.PriorityEscalationWaitMin;

        await _context.SaveChangesAsync();

        await _eventPublisher.PublishAsync(new QueueConfigUpdatedEvent(config.Id, actorUserId));

        return MapToDto(config);
    }

    /// <summary>
    /// Деактивирует конфигурацию очереди (soft delete).
    /// </summary>
    public async Task<QueueConfigDto> DeactivateAsync(int id, int deactivatedById)
    {
        var config = await _context.QueueConfigs
            .FirstOrDefaultAsync(q => q.Id == id);

        if (config == null)
            throw new NotFoundException($"QueueConfig with id {id} not found");

        config.IsActive = false;
        await _context.SaveChangesAsync();

        await _eventPublisher.PublishAsync(new QueueConfigDeactivatedEvent(config.Id, config.Name, deactivatedById));

        return MapToDto(config);
    }

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
            config.CreatedBy?.Login,
            config.CreatedAt
        );
    }

    private QueueConfigDetailDto MapToDetailDto(QueueConfig config)
    {
        var serviceTypes = config.ServiceTypes
            .Where(st => st.IsActive)
            .Select(MapToServiceTypeDto)
            .ToList();

        return new QueueConfigDetailDto(
            MapToDto(config),
            serviceTypes
        );
    }

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

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}
