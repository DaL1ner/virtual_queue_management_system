using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs;

/// <summary>
/// DTO для установки готовности исполнителя
/// </summary>
public record SetExecutorReadyDto(
    int QueueSessionId,
    bool IsReady
);

/// <summary>
/// DTO ответа состояния исполнителя
/// </summary>
public record ExecutorStateDto(
    int Id,
    int QueueSessionId,
    int UserId,
    string UserName,
    bool IsReady,
    int? CurrentTicketId,
    string? CurrentTicketNumber,
    DateTime LastStatusChange,
    int TotalServedCount,
    double? AvgServiceTimeSec
);
