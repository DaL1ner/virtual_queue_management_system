using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs;

using Domain.Enums;

/// <summary>
/// DTO для создания сессии очереди
/// </summary>
public record CreateQueueSessionDto(
    int QueueConfigId,
    string? Description = null
);

/// <summary>
/// DTO для обновления статуса сессии
/// </summary>
public record UpdateQueueSessionStatusDto(
    SessionStatus Status
);

/// <summary>
/// DTO ответа сессии очереди
/// </summary>
public record QueueSessionDto(
    int Id,
    int QueueConfigId,
    string QueueConfigName,
    SessionStatus Status,
    DateTime? StartedAt,
    DateTime? ClosedAt,
    int CreatedById,
    string? CreatedByName,
    DateTime CreatedAt
);

/// <summary>
/// DTO статистики сессии
/// </summary>
public record QueueSessionStatsDto(
    int TotalTickets,
    int WaitingTickets,
    int CalledTickets,
    int ServingTickets,
    int ServedTickets,
    int SkippedTickets,
    int CancelledTickets,
    double? AvgServiceTimeSec,
    TimeSpan? SessionDuration
);