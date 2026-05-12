using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs;

/// <summary>
/// DTO для создания роли
/// </summary>
public record CreateRoleDto(
    string Name,
    string Code,
    string? Description = null
);

/// <summary>
/// DTO ответа роли
/// </summary>
public record RoleDto(
    int Id,
    string Name,
    string Code,
    string? Description,
    DateTime CreatedAt
);
