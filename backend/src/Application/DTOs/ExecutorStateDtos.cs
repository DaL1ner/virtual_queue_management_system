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
    TicketDto? CurrentTicket,
    DateTime LastStatusChange,
    int TotalServedCount,
    double? AvgServiceTimeSec
);

/// <summary>
/// DTO для запроса вызова следующего талона
/// </summary>
public record CallNextTicketDto(
    int? ExecutorUserId = null  // опционально, если null - выбирается случайный готовый исполнитель
);

/// <summary>
/// DTO ответа на вызов следующего талона
/// </summary>
public record CallNextTicketResponseDto(
    TicketDto CalledTicket,
    int AssignedExecutorUserId,
    string AssignedExecutorName
);
