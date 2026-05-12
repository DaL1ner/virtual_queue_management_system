using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs;

/// <summary>
/// Базовый DTO для идентификатора
/// </summary>
public record IdDto(int Id);

/// <summary>
/// DTO для пагинации запросов
/// </summary>
public record PagedRequestDto(
    int Page = 1,
    int PageSize = 20
);

/// <summary>
/// DTO ответа с пагинацией
/// </summary>
public record PagedResponseDto<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
