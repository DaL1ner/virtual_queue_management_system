using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs;

/// <summary>
/// DTO для авторизации (логин)
/// </summary>
public record LoginUserDto(
    string Login,
    string Password
);

/// <summary>
/// DTO для создания пользователя
/// </summary>
public record CreateUserDto(
    string Login,
    string Password,
    string FullName,
    string LastName,
    string? Email = null,
    IEnumerable<int>? RoleIds = null
);

/// <summary>
/// DTO для обновления пользователя
/// </summary>
public record UpdateUserDto(
    string? FullName = null,
    string? LastName = null,
    string? Email = null,
    bool? IsActive = null,
    IEnumerable<int>? RoleIds = null
);

/// <summary>
/// DTO ответа пользователя
/// </summary>
public record UserDto(
    int Id,
    string Login,
    string FullName,
    string LastName,
    string? Email,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IEnumerable<RoleDto> Roles
);

/// <summary>
/// DTO ответа авторизованного пользователя
/// </summary>
public record AuthenticatedUserDto(
    int Id,
    string Login,
    string FullName,
    string LastName,
    string TokenHash,
    DateTime TokenExpiresAt,
    IEnumerable<string> RoleCodes
);
