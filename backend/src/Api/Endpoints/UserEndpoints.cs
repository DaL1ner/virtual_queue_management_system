namespace Api.Endpoints;

using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointGroup = app.MapGroup("/api/users").WithTags("User");

        endpointGroup.MapGet("/", GetAllUsers);
        endpointGroup.MapGet("/{id:int}", GetUserById);
        endpointGroup.MapPost("/", CreateUser);
        endpointGroup.MapPatch("/{id:int}", UpdateUser);
        endpointGroup.MapPost("/{userId:int}/roles/{roleId:int}", AssignRole);
        endpointGroup.MapDelete("/{userId:int}/roles/{roleId:int}", UnassignRole);

        return endpointGroup;
    }

    private static async Task<IResult> GetAllUsers(UserService service)
    {
        var users = await service.GetAllAsync();
        return Results.Ok(users);
    }

    private static async Task<IResult> GetUserById(int id, UserService service)
    {
        var user = await service.GetByIdAsync(id);
        return Results.Ok(user);
    }

    private static async Task<IResult> CreateUser(CreateUserDto dto, UserService service)
    {
        // TODO: Заменить на получение ID из контекста аутентификации
        // Временно используем ID пользователя admin (id = 1)
        var created = await service.CreateAsync(dto, createdById: 1);
        return Results.Created("/api/users/" + created.Id, created);
    }

    private static async Task<IResult> UpdateUser(int id, UpdateUserDto dto, UserService service)
    {
        // TODO: Заменить на получение ID из контекста аутентификации
        // Временно используем ID пользователя admin (id = 1)
        var updated = await service.UpdateAsync(id, dto, updatedById: 1);
        return Results.Ok(updated);
    }

    private static async Task<IResult> AssignRole(int userId, int roleId, UserService service)
    {
        // TODO: Заменить на получение ID из контекста аутентификации
        // Временно используем ID пользователя admin (id = 1)
        var updated = await service.AssignRoleAsync(userId, roleId, assignedById: 1);
        return Results.Ok(updated);
    }

    private static async Task<IResult> UnassignRole(int userId, int roleId, UserService service)
    {
        // TODO: Заменить на получение ID из контекста аутентификации
        // Временно используем ID пользователя admin (id = 1)
        var updated = await service.UnassignRoleAsync(userId, roleId, unassignedById: 1);
        return Results.Ok(updated);
    }
}
