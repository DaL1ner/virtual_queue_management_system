namespace Api.Endpoints;

using Application.Services;
using Application.DTOs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;

public static class ServiceTypeEndpoints
{
    public static IEndpointRouteBuilder MapServiceTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointGroup = app.MapGroup("/api/service-types").WithTags("ServiceType");

        // GET /api/service-types - получение всех типов обслуживания (требует query param queueConfigId)
        endpointGroup.MapGet("/", GetAllServiceTypes);

        // GET /api/service-types/{id} - получение конкретного типа обслуживания
        endpointGroup.MapGet("/{id:int}", GetServiceTypeById);

        // POST /api/service-types - создание типа обслуживания
        endpointGroup.MapPost("/", CreateServiceType);

        // PUT /api/service-types/{id} - редактирование типа обслуживания
        endpointGroup.MapPut("/{id:int}", UpdateServiceType);

        return endpointGroup;
    }

    /// <summary>
    /// Получает все типы обслуживания для указанной конфигурации очереди
    /// </summary>
    private static async Task<IResult> GetAllServiceTypes(
        ServiceTypeService service,
        [FromQuery] int queueConfigId)
    {
        var serviceTypes = await service.GetAllAsync(queueConfigId);
        return Results.Ok(serviceTypes);
    }

    /// <summary>
    /// Получает конкретный тип обслуживания по ID
    /// </summary>
    private static async Task<IResult> GetServiceTypeById(
        int id,
        ServiceTypeService service)
    {
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
        ServiceTypeService service)
    {
        // TODO: Заменить на получение ID из контекста аутентификации
        // Временно используем ID пользователя admin (id = 1)
        var created = await service.CreateAsync(dto, createdById: 1);
        return Results.Created("", created);
    }

    /// <summary>
    /// Редактирует конкретный тип обслуживания
    /// </summary>
    private static async Task<IResult> UpdateServiceType(
        int id,
        UpdateServiceTypeDto dto,
        ServiceTypeService service)
    {
        // TODO: Заменить на получение ID из контекста аутентификации
        // Временно используем ID пользователя admin (id = 1)
        var updated = await service.UpdateAsync(id, dto, actorUserId: 1);
        return Results.Ok(updated);
    }
}
