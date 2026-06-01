namespace Api.Endpoints;

using Application.Services;
using Application.DTOs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Api.Helpers;

public static class ServiceTypeEndpoints
{
    public static IEndpointRouteBuilder MapServiceTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointGroup = app.MapGroup("/api/service-types").WithTags("ServiceType");

        // GET /api/service-types - получение всех типов обслуживания (требует query param queueConfigId)
        endpointGroup.MapGet("/", GetAllServiceTypes);

        // GET /api/service-types/all - получение всех типов обслуживания с информацией о конфигурации
        endpointGroup.MapGet("/all", GetAllServiceTypesWithConfig);

        // GET /api/service-types/{id} - получение конкретного типа обслуживания
        endpointGroup.MapGet("/{id:int}", GetServiceTypeById);

        // POST /api/service-types - создание типа обслуживания
        endpointGroup.MapPost("/", CreateServiceType);

        // PUT /api/service-types/{id} - редактирование типа обслуживания
        endpointGroup.MapPut("/{id:int}", UpdateServiceType);

        // PATCH /api/service-types/{id}/deactivate - деактивация типа обслуживания (soft delete)
        endpointGroup.MapPatch("/{id:int}/deactivate", DeactivateServiceType);

        return endpointGroup;
    }

    /// <summary>
    /// Получает все типы обслуживания для указанной конфигурации очереди
    /// </summary>
    private static async Task<IResult> GetAllServiceTypes(
        ClaimsPrincipal user,
        ServiceTypeService service,
        [FromQuery] int queueConfigId)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();
            
        if (!user.IsInRole("ADMIN"))
            return Results.Forbid();
            
        var serviceTypes = await service.GetAllAsync(queueConfigId);
        return Results.Ok(serviceTypes);
    }

    /// <summary>
    /// Получает все типы обслуживания с информацией о конфигурации очереди
    /// </summary>
    private static async Task<IResult> GetAllServiceTypesWithConfig(
        ClaimsPrincipal user,
        ServiceTypeService service)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();
            
        if (!user.IsInRole("ADMIN"))
            return Results.Forbid();
            
        var serviceTypes = await service.GetAllWithConfigAsync();
        return Results.Ok(serviceTypes);
    }

    /// <summary>
    /// Получает конкретный тип обслуживания по ID
    /// </summary>
    private static async Task<IResult> GetServiceTypeById(
        int id,
        ClaimsPrincipal user,
        ServiceTypeService service)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();
            
        if (!user.IsInRole("ADMIN"))
            return Results.Forbid();
            
        var serviceType = await service.GetByIdAsync(id);
        if (serviceType == null)
            return Results.NotFound();

        return Results.Ok(serviceType);
    }

    /// <summary>
    /// Создаёт новый тип обслуживания
    /// </summary>
    private static async Task<IResult> CreateServiceType(
        CreateServiceTypeDto dto,
        ClaimsPrincipal user,
        ServiceTypeService service)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();
            
        if (!user.IsInRole("ADMIN"))
            return Results.Forbid();
            
        var created = await service.CreateAsync(dto, createdById: userId.Value);
        return Results.Created("", created);
    }

    /// <summary>
    /// Редактирует конкретный тип обслуживания
    /// </summary>
    private static async Task<IResult> UpdateServiceType(
        int id,
        UpdateServiceTypeDto dto,
        ClaimsPrincipal user,
        ServiceTypeService service)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();
            
        if (!user.IsInRole("ADMIN"))
            return Results.Forbid();
            
        var updated = await service.UpdateAsync(id, dto, actorUserId: userId.Value);
        return Results.Ok(updated);
    }

    /// <summary>
    /// Деактивирует тип услуги (soft delete)
    /// </summary>
    private static async Task<IResult> DeactivateServiceType(
        int id,
        ClaimsPrincipal user,
        ServiceTypeService service)
    {
        var userId = user.GetUserId();
        if (userId == null)
            return Results.Unauthorized();
            
        if (!user.IsInRole("ADMIN"))
            return Results.Forbid();
            
        var deactivated = await service.DeactivateAsync(id, deactivatedById: userId.Value);
        return Results.Ok(deactivated);
    }
}
