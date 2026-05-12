using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs;

using Domain.Enums;

/// <summary>
/// DTO фильтра для журнала событий
/// </summary>
public record EventLogFilterDto(
    int? QueueSessionId = null,
    int? TicketId = null,
    EventType? EventType = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
);

/// <summary>
/// DTO ответа события журнала
/// </summary>
public record EventLogDto(
    int Id,
    int QueueSessionId,
    int? TicketId,
    int? ActorUserId,
    string? ActorUserName,
    EventType EventType,
    DateTime Timestamp,
    string? Details
);
