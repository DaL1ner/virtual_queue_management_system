using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs;

/// <summary>
/// DTO для создания клиентской сессии
/// </summary>
public record CreateClientSessionDto(
    string DeviceFingerprint,
    string TokenHash,
    string? IpAddress = null,
    string? UserAgent = null
);

/// <summary>
/// DTO ответа клиентской сессии
/// </summary>
public record ClientSessionDto(
    int Id,
    string DeviceFingerprint,
    string TokenHash,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    bool IsActive,
    int? ActiveTicketId,
    string? ActiveTicketNumber,
    TicketStatus? ActiveTicketStatus
)
{
    public string? ActiveTicketStatusString => ActiveTicketStatus?.ToString();
}

/// <summary>
/// DTO ответа клиентской сессии с оригинальным токеном
/// </summary>
public record ClientSessionWithTokenDto(
    int Id,
    string DeviceFingerprint,
    string Token,
    string TokenHash,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    bool IsActive,
    int? ActiveTicketId,
    string? ActiveTicketNumber,
    TicketStatus? ActiveTicketStatus
)
{
    public string? ActiveTicketStatusString => ActiveTicketStatus?.ToString();
}
