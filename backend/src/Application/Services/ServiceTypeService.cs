namespace Application.Services;

using Domain.Entities;
using Domain.Enums;
using Application.DTOs;
using Application.Events;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Сервис для управления типами обслуживания
/// </summary>
public class ServiceTypeService
{
    private readonly AppDbContext _context;
    private readonly IEventPublisher _eventPublisher;

    public ServiceTypeService(AppDbContext context, IEventPublisher eventPublisher)
    {
        _context = context;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Возвращает все типы обслуживания для указанной конфигурации очереди
    /// </summary>
    public async Task<IEnumerable<ServiceTypeDto>> GetAllAsync(int queueConfigId)
    {
        var serviceTypes = await _context.ServiceTypes
            .Where(st => st.QueueConfigId == queueConfigId)
            .OrderBy(st => st.SortOrder)
            .ToListAsync();

        return serviceTypes.Select(MapToDto);
    }

    /// <summary>
    /// Возвращает все типы услуг с информацией о конфигурации очереди
    /// </summary>
    public async Task<IEnumerable<ServiceTypeWithConfigDto>> GetAllWithConfigAsync()
    {
        var serviceTypes = await _context.ServiceTypes
            .Include(st => st.QueueConfig)
            .OrderBy(st => st.QueueConfigId)
            .ThenBy(st => st.SortOrder)
            .ToListAsync();

        return serviceTypes.Select(st => new ServiceTypeWithConfigDto(
            st.Id,
            st.QueueConfigId,
            st.QueueConfig?.Name ?? "Unknown",
            st.Name,
            st.Code,
            st.Letter,
            st.BasePriorityLevel,
            st.PlanAvgServiceTimeSec,
            st.IsActive,
            st.IsHighlighting,
            st.SortOrder,
            st.CreatedAt
        ));
    }

    /// <summary>
    /// Возвращает тип обслуживания по ID
    /// </summary>
    public async Task<ServiceTypeDto?> GetByIdAsync(int id)
    {
        var serviceType = await _context.ServiceTypes
            .FirstOrDefaultAsync(st => st.Id == id);

        if (serviceType == null)
            return null;

        return MapToDto(serviceType);
    }

    /// <summary>
    /// Создаёт новый тип обслуживания
    /// </summary>
    public async Task<ServiceTypeDto> CreateAsync(CreateServiceTypeDto dto, int createdById)
    {
        // Валидация: проверка существования конфигурации очереди
        var queueConfig = await _context.QueueConfigs
            .FirstOrDefaultAsync(qc => qc.Id == dto.QueueConfigId);

        if (queueConfig == null)
        {
            throw new NotFoundException($"QueueConfig with id {dto.QueueConfigId} not found");
        }

        // Валидация: проверка что типы услуг включены для конфигурации
        if (!queueConfig.IsServiceTypeEnabled)
        {
            throw new BadRequestException("Service types are disabled for this queue configuration");
        }

        // Валидация: проверка уникальности буквы для конфигурации
        var existsWithSameLetter = await _context.ServiceTypes
            .AnyAsync(st => st.QueueConfigId == dto.QueueConfigId && st.Letter == dto.Letter);

        if (existsWithSameLetter)
        {
            throw new ConflictException($"Service type with letter '{dto.Letter}' already exists for this queue configuration");
        }

        // Валидация: проверка что код уникален
        var existsWithSameCode = await _context.ServiceTypes
            .AnyAsync(st => st.Code == dto.Code);

        if (existsWithSameCode)
        {
            throw new ConflictException($"Service type with code '{dto.Code}' already exists");
        }

        var serviceType = new ServiceType
        {
            QueueConfigId = dto.QueueConfigId,
            Name = dto.Name,
            Code = dto.Code,
            Letter = dto.Letter,
            BasePriorityLevel = dto.BasePriorityLevel,
            PlanAvgServiceTimeSec = dto.PlanAvgServiceTimeSec,
            IsActive = dto.IsActive,
            IsHighlighting = dto.IsHighlighting,
            SortOrder = dto.SortOrder,
            CreatedAt = DateTime.UtcNow
        };

        _context.ServiceTypes.Add(serviceType);
        await _context.SaveChangesAsync();

        // Публикация события
        await _eventPublisher.PublishAsync(new ServiceTypeCreatedEvent(
            serviceType.Id,
            serviceType.QueueConfigId,
            serviceType.Name,
            createdById
        ));

        return MapToDto(serviceType);
    }

    /// <summary>
    /// Обновляет тип обслуживания
    /// </summary>
    public async Task<ServiceTypeDto> UpdateAsync(int id, UpdateServiceTypeDto dto, int actorUserId)
    {
        var serviceType = await _context.ServiceTypes
            .FirstOrDefaultAsync(st => st.Id == id);

        if (serviceType == null)
        {
            throw new NotFoundException($"ServiceType with id {id} not found");
        }

        // Валидация: проверка что конфигурация очереди существует
        var queueConfig = await _context.QueueConfigs
            .FirstOrDefaultAsync(qc => qc.Id == serviceType.QueueConfigId);

        if (queueConfig == null)
        {
            throw new NotFoundException($"QueueConfig with id {serviceType.QueueConfigId} not found");
        }

        // Валидация: проверка что типы услуг включены для конфигурации
        if (!queueConfig.IsServiceTypeEnabled)
        {
            throw new BadRequestException("Service types are disabled for this queue configuration");
        }

        // Валидация: проверка уникальности кода (если код изменён)
        if (dto.Code != null && dto.Code != serviceType.Code)
        {
            var existsWithSameCode = await _context.ServiceTypes
                .AnyAsync(st => st.Code == dto.Code);

            if (existsWithSameCode)
            {
                throw new ConflictException($"Service type with code '{dto.Code}' already exists");
            }
        }

        // Валидация: проверка уникальности буквы (если буква изменена)
        if (dto.Letter.HasValue && dto.Letter.Value != serviceType.Letter)
        {
            var existsWithSameLetter = await _context.ServiceTypes
                .AnyAsync(st => st.QueueConfigId == serviceType.QueueConfigId 
                             && st.Letter == dto.Letter.Value 
                             && st.Id != id);

            if (existsWithSameLetter)
            {
                throw new ConflictException($"Service type with letter '{dto.Letter.Value}' already exists for this queue configuration");
            }
        }

        // Обновление полей
        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            serviceType.Name = dto.Name;
        }

        if (dto.Code != null)
        {
            serviceType.Code = dto.Code;
        }

        if (dto.Letter.HasValue)
        {
            serviceType.Letter = dto.Letter.Value;
        }

        if (dto.BasePriorityLevel.HasValue)
        {
            if (dto.BasePriorityLevel.Value < 0)
            {
                throw new BadRequestException("BasePriorityLevel must be >= 0");
            }
            serviceType.BasePriorityLevel = dto.BasePriorityLevel.Value;
        }

        if (dto.PlanAvgServiceTimeSec.HasValue)
        {
            if (dto.PlanAvgServiceTimeSec.Value <= 0)
            {
                throw new BadRequestException("PlanAvgServiceTimeSec must be > 0");
            }
            serviceType.PlanAvgServiceTimeSec = dto.PlanAvgServiceTimeSec.Value;
        }

        if (dto.IsActive.HasValue)
        {
            serviceType.IsActive = dto.IsActive.Value;
        }

        if (dto.IsHighlighting.HasValue)
        {
            serviceType.IsHighlighting = dto.IsHighlighting.Value;
        }

        if (dto.SortOrder.HasValue)
        {
            if (dto.SortOrder.Value < 0)
            {
                throw new BadRequestException("SortOrder must be >= 0");
            }
            serviceType.SortOrder = dto.SortOrder.Value;
        }

        await _context.SaveChangesAsync();

        // Публикация события
        await _eventPublisher.PublishAsync(new ServiceTypeUpdatedEvent(
            serviceType.Id,
            serviceType.QueueConfigId,
            actorUserId
        ));

        return MapToDto(serviceType);
    }

    /// <summary>
    /// Возвращает все типы обслуживания для указанной сессии очереди
    /// </summary>
    public async Task<IEnumerable<ServiceTypeDto>> GetAllBySessionIdAsync(int sessionId)
    {
        var session = await _context.QueueSessions
            .Include(q => q.QueueConfig)
            .FirstOrDefaultAsync(q => q.Id == sessionId);

        if (session == null)
            throw new NotFoundException($"QueueSession with id {sessionId} not found");

        return await GetAllAsync(session.QueueConfigId);
    }

    /// <summary>
    /// Преобразование в ServiceTypeDto
    /// </summary>
    private ServiceTypeDto MapToDto(ServiceType serviceType)
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
