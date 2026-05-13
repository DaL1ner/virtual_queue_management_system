namespace Application.Services;

using System.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;

/// <summary>
/// Интерфейс для работы с контекстом базы данных
/// </summary>
public interface IDbContext
{
    DbSet<User> Users { get; }
    DbSet<QueueConfig> QueueConfigs { get; }
    DbSet<QueueSession> QueueSessions { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<ServiceType> ServiceTypes { get; }
    DbSet<ExecutorState> ExecutorStates { get; }
    DbSet<ClientSession> ClientSessions { get; }
    DbSet<EventLog> EventLogs { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<UserSession> UserSessions { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters);
}
