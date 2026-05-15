using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs;

using Domain.Enums;

/// <summary>
/// DTO для создания талона (клиентом)
/// </summary>
public record CreateTicketDto(
    string ClientName,
    string ClientSurname,
    int? ServiceTypeId = null
);

/// <summary>
/// DTO для вызова талона (оператором)
/// </summary>
public record CallTicketDto(
    int TicketId
);

/// <summary>
/// DTO для начала обслуживания
/// </summary>
public record StartServiceDto(
    int TicketId
);

/// <summary>
/// DTO для завершения обслуживания
/// </summary>
public record CompleteServiceDto(
    int TicketId,
    bool Success = true
);

/// <summary>
/// DTO для отмены талона
/// </summary>
public record CancelTicketDto(
    int TicketId,
    string Reason
);

/// <summary>
/// DTO для изменения приоритета
/// </summary>
public record ChangePriorityDto(
    int TicketId,
    int PriorityLevel
);

/// <summary>
/// DTO ответа талона
/// </summary>
public record TicketDto(
    int Id,
    int QueueSessionId,
    string TicketNumber,
    string ClientName,
    string ClientSurname,
    int? ServiceTypeId,
    string? ServiceTypeName,
    char? ServiceLetter,
    int SortOrder,
    int PriorityLevel,
    TicketStatus Status,
    int Version,
    DateTime CreatedAt,
    DateTime? CalledAt,
    DateTime? ServiceStartedAt,
    DateTime? ServiceEndedAt,
    int? ServedByUserId,
    string? ServedByUserName,
    string? CancelReason,
    int PositionInQueue
);

/// <summary>
/// DTO для списка талонов в очереди
/// </summary>
public record TicketListDto(
    IEnumerable<TicketDto> Tickets,
    int TotalCount,
    int YourPosition
);

/// <summary>
/// DTO для перемещения талона на N шагов назад (дальше от начала очереди)
/// </summary>
public record MoveTicketBackwardDto(
    int TicketId,
    int Steps,
    int? ActorUserId = null
);

/// <summary>
/// DTO для ответа с позицией талона в очереди
/// </summary>
public record TicketPositionDto(
    int TicketId,
    int Position,
    int TotalWaiting,
    int? EstimatedWaitMinutes = null
);
