using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs;

/// <summary>
/// DTO для создания типа услуги
/// </summary>
public record CreateServiceTypeDto(
    int QueueConfigId,
    string Name,
    string Code,
    char Letter,
    int BasePriorityLevel = 0,
    int? PlanAvgServiceTimeSec = null,
    bool IsActive = true,
    bool IsHighlighting = false,
    int SortOrder = 0
);

/// <summary>
/// DTO для обновления типа услуги
/// </summary>
public record UpdateServiceTypeDto(
    string? Name = null,
    string? Code = null,
    char? Letter = null,
    int? BasePriorityLevel = null,
    int? PlanAvgServiceTimeSec = null,
    bool? IsActive = null,
    bool? IsHighlighting = null,
    int? SortOrder = null
);

/// <summary>
/// DTO ответа типа услуги
/// </summary>
public record ServiceTypeDto(
    int Id,
    int QueueConfigId,
    string Name,
    string Code,
    char Letter,
    int BasePriorityLevel,
    int? PlanAvgServiceTimeSec,
    bool IsActive,
    bool IsHighlighting,
    int SortOrder,
    DateTime CreatedAt
);