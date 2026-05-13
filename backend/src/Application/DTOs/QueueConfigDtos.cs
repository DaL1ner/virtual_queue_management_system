namespace Application.DTOs;

using Domain.Enums;

/// <summary>
/// DTO для создания конфигурации очереди
/// </summary>
public record CreateQueueConfigDto(
    string Name,
    string? Description,
    DistributionMode DistributionMode = DistributionMode.Manual,
    bool IsServiceTypeEnabled = false,
    bool IsPriorityEnabled = true,
    int? PriorityEscalationWaitMin = null
);

/// <summary>
/// DTO для обновления конфигурации очереди
/// </summary>
public record UpdateQueueConfigDto(
    string? Name = null,
    string? Description = null,
    DistributionMode? DistributionMode = null,
    bool? IsServiceTypeEnabled = null,
    bool? IsPriorityEnabled = null,
    int? PriorityEscalationWaitMin = null
);

/// <summary>
/// DTO ответа конфигурации очереди
/// </summary>
public record QueueConfigDto(
    int Id,
    string Name,
    string? Description,
    DistributionMode DistributionMode,
    bool IsServiceTypeEnabled,
    bool IsPriorityEnabled,
    int? PriorityEscalationWaitMin,
    bool IsActive,
    int CreatedById,
    string? CreatedByName,
    DateTime CreatedAt
);

/// <summary>
/// DTO детальной информации конфигурации с типами услуг
/// </summary>
public record QueueConfigDetailDto(
    QueueConfigDto Config,
    IEnumerable<ServiceTypeDto> ServiceTypes
);
