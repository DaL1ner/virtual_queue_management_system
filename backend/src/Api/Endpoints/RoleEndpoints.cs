namespace Api.Endpoints;

using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using Api.Helpers;

public static class RoleEndpoints
{
    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointGroup = app.MapGroup("/api/roles").WithTags("Role");

        endpointGroup.MapGet("/", GetAllRoles);

        return endpointGroup;
    }

    private static async Task<IResult> GetAllRoles(
        ClaimsPrincipal user,
        RoleService service)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();

        if (!user.IsInRole("ADMIN"))
            return Results.Forbid();

        var roles = await service.GetAllAsync();
        return Results.Ok(roles);
    }
}