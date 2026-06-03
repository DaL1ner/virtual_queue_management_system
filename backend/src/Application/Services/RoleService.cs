namespace Application.Services;

using Application.DTOs;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Сервис для управления ролями
/// </summary>
public class RoleService
{
    private readonly AppDbContext _context;

    public RoleService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Получение списка всех ролей
    /// </summary>
    public async Task<IEnumerable<RoleDto>> GetAllAsync()
    {
        var roles = await _context.Roles
            .OrderBy(r => r.Name)
            .ToListAsync();

        return roles.Select(r => new RoleDto(
            r.Id,
            r.Name,
            r.Code,
            r.Description,
            r.CreatedAt
        ));
    }
}