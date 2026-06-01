namespace Api.Endpoints;

using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using Api.Helpers;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointGroup = app.MapGroup("/api/users").WithTags("User");

        endpointGroup.MapGet("/", GetAllUsers);
        endpointGroup.MapGet("/me", GetMyUser);
        endpointGroup.MapGet("/{id:int}", GetUserById);
        endpointGroup.MapPost("/", CreateUser);
        endpointGroup.MapPatch("/{id:int}", UpdateUser);
        endpointGroup.MapPatch("/{id:int}/deactivate", DeactivateUser);
        endpointGroup.MapPatch("/{id:int}/activate", ActivateUser);
        endpointGroup.MapPost("/{userId:int}/roles/{roleId:int}", AssignRole);
        endpointGroup.MapDelete("/{userId:int}/roles/{roleId:int}", UnassignRole);

        return endpointGroup;
    }

    private static async Task<IResult> GetAllUsers(
        ClaimsPrincipal user,
        UserService service)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();
            
        if (!user.IsInRole("ADMIN"))
            return Results.Forbid();
            
        var users = await service.GetAllAsync(isActive: true);
        return Results.Ok(users);
    }

    private static async Task<IResult> GetUserById(
        int id,
        ClaimsPrincipal user,
        UserService service)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();
            
        if (!user.IsInRole("ADMIN"))
            return Results.Forbid();
            
        var userDto = await service.GetByIdAsync(id);
        return Results.Ok(userDto);
    }

    private static async Task<IResult> CreateUser(
        CreateUserDto dto,
        ClaimsPrincipal user,
        UserService service)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();
            
        if (!user.IsInRole("ADMIN"))
            return Results.Forbid();
            
        var created = await service.CreateAsync(dto, createdById: userId.Value);
        return Results.Created("/api/users/" + created.Id, created);
    }

    private static async Task<IResult> UpdateUser(
        int id,
        UpdateUserDto dto,
        ClaimsPrincipal user,
        UserService service)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();
            
        if (!user.IsInRole("ADMIN"))
            return Results.Forbid();
            
        var updated = await service.UpdateAsync(id, dto, updatedById: userId.Value);
        return Results.Ok(updated);
    }

    /// <summary>
    /// Деактивирует учётную запись пользователя (soft delete)
    /// </summary>
    private static async Task<IResult> DeactivateUser(
        int id,
        ClaimsPrincipal user,
        UserService service)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();
            
        if (!user.IsInRole("ADMIN"))
            return Results.Forbid();
            
        var deactivated = await service.DeactivateAsync(id, deactivatedById: userId.Value);
        return Results.Ok(deactivated);
    }

    /// <summary>
    /// Активирует учётную запись пользователя
    /// </summary>
    private static async Task<IResult> ActivateUser(
        int id,
        ClaimsPrincipal user,
        UserService service)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();
            
        if (!user.IsInRole("ADMIN"))
            return Results.Forbid();
            
        var activated = await service.ActivateAsync(id, activatedById: userId.Value);
        return Results.Ok(activated);
    }

    private static async Task<IResult> AssignRole(
        int userId,
        int roleId,
        ClaimsPrincipal user,
        UserService service)
    {
        var actorId = user.GetUserId();
        if (actorId == null)
            return Results.Unauthorized();
            
        if (!user.IsInRole("ADMIN"))
            return Results.Forbid();
            
        var updated = await service.AssignRoleAsync(userId, roleId, assignedById: actorId.Value);
        return Results.Ok(updated);
    }

    private static async Task<IResult> UnassignRole(
        int userId,
        int roleId,
        ClaimsPrincipal user,
        UserService service)
    {
        var actorId = user.GetUserId();
        if (actorId == null)
            return Results.Unauthorized();
            
        if (!user.IsInRole("ADMIN"))
            return Results.Forbid();
            
        var updated = await service.UnassignRoleAsync(userId, roleId, unassignedById: actorId.Value);
        return Results.Ok(updated);
    }

    /// <summary>
    /// Получение информации о текущем авторизованном пользователе
    /// </summary>
    private static async Task<IResult> GetMyUser(
        ClaimsPrincipal user,
        UserService service)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();
            
        if (!user.IsInAnyRole("EXECUTOR", "OPERATOR", "ADMIN"))
            return Results.Forbid();
            
        var userDto = await service.GetByIdAsync(userId.Value);
        return Results.Ok(userDto);
    }
}
