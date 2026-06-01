using Application.DTOs;
using Application.Events;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

/// <summary>
/// Сервис для управления типами услуг.
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
    /// Получает все типы услуг для указанной конфигурации очереди.
    /// </summary>
    public async Task<IEnumerable<ServiceTypeDto>> GetAllAsync(int queueConfigId)
    {
        var serviceTypes = await _context.ServiceTypes
            .Where(st => st.QueueConfigId == queueConfigId && st.IsActive)
            .ToListAsync();

        return serviceTypes.Select(MapToDto);
    }

    /// <summary>
    /// Получает все типы услуг с информацией о конфигурации очереди.
    /// </summary>
    public async Task<IEnumerable<ServiceTypeWithConfigDto>> GetAllWithConfigAsync()
    {
        var serviceTypes = await _context.ServiceTypes
            .Include(st => st.QueueConfig)
            .Where(st => st.IsActive)
            .ToListAsync();

        return serviceTypes.Select(st => new ServiceTypeWithConfigDto(
            st.Id,
            st.QueueConfigId,
            st.QueueConfig!.Name,
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
    /// Получает тип услуги по ID.
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
    /// Создаёт новый тип услуги.
    /// </summary>
    public async Task<ServiceTypeDto> CreateAsync(CreateServiceTypeDto dto, int createdById)
    {
        var serviceType = new ServiceType
        {
            QueueConfigId = dto.QueueConfigId,
            Name = dto.Name,
            Code = dto.Code,
            Letter = dto.Letter,
            BasePriorityLevel = dto.BasePriorityLevel,
            PlanAvgServiceTimeSec = dto.PlanAvgServiceTimeSec,
            IsHighlighting = dto.IsHighlighting,
            SortOrder = dto.SortOrder,
            IsActive = true
        };

        _context.ServiceTypes.Add(serviceType);
        await _context.SaveChangesAsync();

        await _eventPublisher.PublishAsync(new ServiceTypeCreatedEvent(serviceType.Id, serviceType.QueueConfigId, serviceType.Name, createdById));

        return MapToDto(serviceType);
    }

    /// <summary>
    /// Обновляет существующий тип услуги.
    /// </summary>
    public async Task<ServiceTypeDto> UpdateAsync(int id, UpdateServiceTypeDto dto, int actorUserId)
    {
        var serviceType = await _context.ServiceTypes
            .FirstOrDefaultAsync(st => st.Id == id);

        if (serviceType == null)
            throw new Application.Services.NotFoundException($"ServiceType with id {id} not found");

        if (dto.Name is not null) serviceType.Name = dto.Name;
        if (dto.Code is not null) serviceType.Code = dto.Code;
        if (dto.Letter.HasValue) serviceType.Letter = dto.Letter.Value;
        if (dto.BasePriorityLevel is not null) serviceType.BasePriorityLevel = dto.BasePriorityLevel.Value;
        if (dto.PlanAvgServiceTimeSec is not null) serviceType.PlanAvgServiceTimeSec = dto.PlanAvgServiceTimeSec;
        if (dto.IsHighlighting is not null) serviceType.IsHighlighting = dto.IsHighlighting.Value;
        if (dto.SortOrder is not null) serviceType.SortOrder = dto.SortOrder.Value;

        await _context.SaveChangesAsync();

        await _eventPublisher.PublishAsync(new ServiceTypeUpdatedEvent(serviceType.Id, serviceType.QueueConfigId, actorUserId));

        return MapToDto(serviceType);
    }

    /// <summary>
    /// Деактивирует тип услуги (soft delete).
    /// </summary>
    public async Task<ServiceTypeDto> DeactivateAsync(int id, int deactivatedById)
    {
        var serviceType = await _context.ServiceTypes
            .FirstOrDefaultAsync(st => st.Id == id);

        if (serviceType == null)
            throw new Application.Services.NotFoundException($"ServiceType with id {id} not found");

        serviceType.IsActive = false;
        await _context.SaveChangesAsync();

        await _eventPublisher.PublishAsync(new ServiceTypeDeactivatedEvent(serviceType.Id, serviceType.QueueConfigId, serviceType.Name, deactivatedById));

        return MapToDto(serviceType);
    }

    /// <summary>
    /// Получает все типы услуг для указанного SessionId.
    /// </summary>
    public async Task<IEnumerable<ServiceTypeDto>> GetAllBySessionIdAsync(int sessionId)
    {
        var queueSession = await _context.QueueSessions
            .FirstOrDefaultAsync(qs => qs.Id == sessionId);

        if (queueSession == null)
            throw new Application.Services.NotFoundException($"QueueSession with id {sessionId} not found");

        var serviceTypes = await _context.ServiceTypes
            .Where(st => st.QueueConfigId == queueSession.QueueConfigId && st.IsActive)
            .ToListAsync();

        return serviceTypes.Select(MapToDto);
    }

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
