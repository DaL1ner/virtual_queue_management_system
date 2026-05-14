namespace Application.Services;

using Domain.Entities;
using Application.DTOs;
using Application.Events;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Сервис для управления учётными записями сотрудников
/// </summary>
public class UserService
{
    private readonly AppDbContext _context;
    private readonly IEventPublisher _eventPublisher;

    public UserService(AppDbContext context, IEventPublisher eventPublisher)
    {
        _context = context;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Создание учётной записи с назначением ролей
    /// </summary>
    public async Task<UserDto> CreateAsync(CreateUserDto dto, int createdById)
    {
        // Проверка уникальности login
        var exists = await _context.Users
            .AnyAsync(u => u.Login == dto.Login);

        if (exists)
        {
            throw new ConflictException($"User with login '{dto.Login}' already exists");
        }

        // Создание пользователя
        var user = new User
        {
            Login = dto.Login,
            PasswordHash = dto.Password, // TODO: Хешировать пароль
            FullName = dto.FullName,
            LastName = dto.LastName,
            Email = dto.Email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Назначение ролей
        if (dto.RoleIds != null && dto.RoleIds.Any())
        {
            foreach (var roleId in dto.RoleIds)
            {
                var role = await _context.Roles.FindAsync(roleId);
                if (role != null)
                {
                    var userRole = new UserRole
                    {
                        UserId = user.Id,
                        RoleId = role.Id,
                        AssignedAt = DateTime.UtcNow,
                        AssignedBy = createdById
                    };
                    _context.UserRoles.Add(userRole);
                }
            }
            await _context.SaveChangesAsync();
        }

        // Публикация события
        await _eventPublisher.PublishAsync(new UserCreatedEvent(user.Id, user.Login, createdById));

        return await GetByIdAsync(user.Id);
    }

    /// <summary>
    /// Получение списка учётных записей
    /// </summary>
    public async Task<IEnumerable<UserDto>> GetAllAsync(bool? isActive = null)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        return users.Select(MapToDto);
    }

    /// <summary>
    /// Получение учётной записи с детальной информацией
    /// </summary>
    public async Task<UserDto> GetByIdAsync(int id)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            throw new NotFoundException($"User with id {id} not found");
        }

        return MapToDto(user);
    }

    /// <summary>
    /// Обновление учётной записи
    /// </summary>
    public async Task<UserDto> UpdateAsync(int id, UpdateUserDto dto, int updatedById)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            throw new NotFoundException($"User with id {id} not found");
        }

        // Обновление полей
        if (!string.IsNullOrWhiteSpace(dto.FullName))
        {
            user.FullName = dto.FullName;
        }

        if (!string.IsNullOrWhiteSpace(dto.LastName))
        {
            user.LastName = dto.LastName;
        }

        if (dto.Email != null)
        {
            // Проверка уникальности email
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == dto.Email && u.Id != id);

            if (emailExists)
            {
                throw new ConflictException($"User with email '{dto.Email}' already exists");
            }

            user.Email = dto.Email;
        }

        if (dto.IsActive.HasValue)
        {
            user.IsActive = dto.IsActive.Value;
        }

        await _context.SaveChangesAsync();

        // Публикация события
        await _eventPublisher.PublishAsync(new UserUpdatedEvent(user.Id, user.Login, updatedById));

        return MapToDto(user);
    }

    /// <summary>
    /// Назначение роли пользователю
    /// </summary>
    public async Task<UserDto> AssignRoleAsync(int userId, int roleId, int assignedById)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new NotFoundException($"User with id {userId} not found");
        }

        var role = await _context.Roles.FindAsync(roleId);
        if (role == null)
        {
            throw new NotFoundException($"Role with id {roleId} not found");
        }

        // Проверка, не назначена ли уже роль
        var exists = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (exists)
        {
            throw new ConflictException($"Role '{role.Name}' is already assigned to user '{user.Login}'");
        }

        // Создание UserRole
        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = assignedById
        };

        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync();

        // Публикация события
        await _eventPublisher.PublishAsync(new UserRoleAssignedEvent(userId, roleId, role.Name, assignedById));

        return MapToDto(user);
    }

    /// <summary>
    /// Снятие роли у пользователя
    /// </summary>
    public async Task<UserDto> UnassignRoleAsync(int userId, int roleId, int unassignedById)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new NotFoundException($"User with id {userId} not found");
        }

        var role = await _context.Roles.FindAsync(roleId);
        if (role == null)
        {
            throw new NotFoundException($"Role with id {roleId} not found");
        }

        // Поиск UserRole
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (userRole == null)
        {
            throw new NotFoundException($"Role '{role.Name}' is not assigned to user '{user.Login}'");
        }

        // Удаление UserRole
        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync();

        // Публикация события
        await _eventPublisher.PublishAsync(new UserRoleUnassignedEvent(userId, roleId, role.Name, unassignedById));

        return MapToDto(user);
    }

    /// <summary>
    /// Преобразование User в UserDto
    /// </summary>
    private UserDto MapToDto(User user)
    {
        var roles = user.UserRoles
            .Select(ur => new RoleDto(
                ur.Role.Id,
                ur.Role.Name,
                ur.Role.Code,
                ur.Role.Description,
                ur.Role.CreatedAt
            ));

        return new UserDto(
            user.Id,
            user.Login,
            user.FullName,
            user.LastName,
            user.Email,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt,
            roles
        );
    }
}
