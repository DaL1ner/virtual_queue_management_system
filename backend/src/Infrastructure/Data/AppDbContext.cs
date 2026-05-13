using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Entities
    public DbSet<User> Users => Set<User>();
    public DbSet<QueueConfig> QueueConfigs => Set<QueueConfig>();
    public DbSet<QueueSession> QueueSessions => Set<QueueSession>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<ExecutorState> ExecutorStates => Set<ExecutorState>();
    public DbSet<ClientSession> ClientSessions => Set<ClientSession>();
    public DbSet<EventLog> EventLogs => Set<EventLog>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply all configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        // Configure relationships
        modelBuilder.Entity<QueueConfig>(entity =>
        {
            entity.ToTable("queue_configs");
            entity.HasKey(qc => qc.Id);
            
            entity.Property(qc => qc.Id)
                .HasColumnName("id");
            
            entity.Property(qc => qc.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(qc => qc.Description)
                .HasColumnName("description");
            
            entity.HasIndex(qc => qc.Name);
            
            entity.Property(qc => qc.DistributionMode)
                .HasColumnName("distribution_mode")
                .HasConversion<string>()
                .HasMaxLength(50);
            
            entity.Property(qc => qc.IsServiceTypeEnabled)
                .HasColumnName("is_service_type_enabled")
                .HasDefaultValue(false);
            
            entity.Property(qc => qc.IsPriorityEnabled)
                .HasColumnName("is_priority_enabled")
                .HasDefaultValue(true);
            
            entity.Property(qc => qc.PriorityEscalationWaitMin)
                .HasColumnName("priority_escalation_wait_min");
            
            entity.Property(qc => qc.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);
            
            entity.Property(qc => qc.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");
            
            entity.Property(qc => qc.CreatedById)
                .HasColumnName("created_by_id");
        });
        
        modelBuilder.Entity<QueueSession>(entity =>
        {
            entity.ToTable("queue_sessions");
            entity.HasKey(qs => qs.Id);
            
            entity.Property(qs => qs.Id)
                .HasColumnName("id");
            
            entity.Property(qs => qs.QueueConfigId)
                .HasColumnName("queue_config_id");
            
            entity.Property(qs => qs.Status)
                .HasColumnName("status")
                .HasConversion<int>();
            
            entity.Property(qs => qs.StartedAt)
                .HasColumnName("started_at");
            
            entity.Property(qs => qs.ClosedAt)
                .HasColumnName("closed_at");
            
            entity.Property(qs => qs.CreatedById)
                .HasColumnName("created_by");
            
            entity.Property(qs => qs.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");
            
            entity.HasIndex(qs => qs.QueueConfigId);
            
            // Unique constraint: only one OPEN session per config
            entity.HasIndex(qs => new { qs.QueueConfigId, qs.Status })
                .HasName("uq_queue_sessions_one_open_per_config")
                .IsUnique()
                .HasFilter("WHERE status = 1");
        });
        
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("tickets");
            entity.HasKey(t => t.Id);
            
            entity.Property(t => t.Id)
                .HasColumnName("id");
            
            entity.Property(t => t.QueueSessionId)
                .HasColumnName("queue_session_id");
            
            entity.Property(t => t.ServiceTypeId)
                .HasColumnName("service_type_id");
            
            entity.Property(t => t.TicketNumber)
                .HasColumnName("ticket_number")
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(t => t.ClientName)
                .HasColumnName("client_name")
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(t => t.ClientSurname)
                .HasColumnName("client_surname")
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(t => t.SortOrder)
                .HasColumnName("sort_order")
                .HasColumnType("decimal");
            
            entity.Property(t => t.PriorityLevel)
                .HasColumnName("priority_level")
                .HasDefaultValue(0);
            
            entity.Property(t => t.Status)
                .HasColumnName("status")
                .HasConversion<int>();
            
            entity.Property(t => t.Version)
                .HasColumnName("version")
                .HasDefaultValue(1);
            
            entity.Property(t => t.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");
            
            entity.Property(t => t.CalledAt)
                .HasColumnName("called_at");
            
            entity.Property(t => t.ServiceStartedAt)
                .HasColumnName("service_started_at");
            
            entity.Property(t => t.ServiceEndedAt)
                .HasColumnName("service_ended_at");
            
            entity.Property(t => t.UpdatedAt)
                .HasColumnName("updated_at");
            
            entity.Property(t => t.ServedByUserId)
                .HasColumnName("served_by_user_id");
            
            entity.Property(t => t.ClientSessionId)
                .HasColumnName("client_session_id");
            
            entity.Property(t => t.CancelReason)
                .HasColumnName("cancel_reason");
            
            // Index for queue display sorting
            entity.HasIndex(t => new { t.QueueSessionId, t.PriorityLevel, t.SortOrder, t.CreatedAt })
                .HasName("idx_ticket_queue_sort");
            
            // Unique constraint: only one active ticket per client session
            entity.HasIndex(t => new { t.ClientSessionId, t.Status })
                .HasName("uq_tickets_one_active_per_client_session")
                .IsUnique()
                .HasFilter("WHERE status IN (0, 1, 2)");
        });
        
        modelBuilder.Entity<ServiceType>(entity =>
        {
            entity.ToTable("service_types");
            entity.HasKey(st => st.Id);
            
            entity.Property(st => st.Id)
                .HasColumnName("id");
            
            entity.Property(st => st.QueueConfigId)
                .HasColumnName("queue_config_id");
            
            entity.Property(st => st.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(st => st.Code)
                .HasColumnName("code")
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(st => st.Letter)
                .HasColumnName("letter")
                .IsRequired()
                .HasMaxLength(1);
            
            entity.Property(st => st.BasePriorityLevel)
                .HasColumnName("base_priority_level")
                .HasDefaultValue(0);
            
            entity.Property(st => st.PlanAvgServiceTimeSec)
                .HasColumnName("plan_avg_service_time_sec");
            
            entity.Property(st => st.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);
            
            entity.Property(st => st.IsHighlighting)
                .HasColumnName("is_highlighting")
                .HasDefaultValue(false);
            
            entity.Property(st => st.SortOrder)
                .HasColumnName("sort_order")
                .HasDefaultValue(0);
            
            entity.Property(st => st.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");
            
            entity.HasIndex(st => st.QueueConfigId);
            
            // Unique letter per config
            entity.HasIndex(st => new { st.QueueConfigId, st.Letter })
                .HasName("uq_service_types_config_letter")
                .IsUnique();
        });
        
        modelBuilder.Entity<ExecutorState>(entity =>
        {
            entity.ToTable("executor_states");
            entity.HasKey(es => es.Id);
            
            entity.Property(es => es.Id)
                .HasColumnName("id");
            
            entity.Property(es => es.QueueSessionId)
                .HasColumnName("queue_session_id");
            
            entity.Property(es => es.UserId)
                .HasColumnName("user_id");
            
            entity.Property(es => es.IsReady)
                .HasColumnName("is_ready")
                .HasDefaultValue(false);
            
            entity.Property(es => es.CurrentTicketId)
                .HasColumnName("current_ticket_id");
            
            entity.Property(es => es.LastStatusChange)
                .HasColumnName("last_status_change")
                .HasDefaultValueSql("NOW()");
            
            entity.HasIndex(es => es.QueueSessionId);
            
            // Ready Executors Index
            entity.HasIndex(es => new { es.QueueSessionId, es.IsReady })
                .HasName("idx_executor_ready");
        });
        
        modelBuilder.Entity<ClientSession>(entity =>
        {
            entity.ToTable("client_sessions");
            entity.HasKey(cs => cs.Id);
            
            entity.Property(cs => cs.Id)
                .HasColumnName("id");
            
            entity.Property(cs => cs.DeviceFingerprint)
                .HasColumnName("device_fingerprint")
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(cs => cs.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");
            
            entity.Property(cs => cs.ExpiresAt)
                .HasColumnName("expires_at");
            
            entity.Property(cs => cs.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);
            
            entity.Property(cs => cs.IpAddress)
                .HasColumnName("ip_address");
            
            entity.Property(cs => cs.UserAgent)
                .HasColumnName("user_agent");
            
            entity.HasIndex(cs => cs.DeviceFingerprint);
        });
        
        modelBuilder.Entity<EventLog>(entity =>
        {
            entity.ToTable("event_logs");
            entity.HasKey(el => el.Id);
            
            entity.Property(el => el.Id)
                .HasColumnName("id");
            
            entity.Property(el => el.QueueSessionId)
                .HasColumnName("queue_session_id");
            
            entity.Property(el => el.TicketId)
                .HasColumnName("ticket_id");
            
            entity.Property(el => el.ActorUserId)
                .HasColumnName("actor_user_id");
            
            entity.Property(el => el.EventType)
                .HasColumnName("event_type")
                .HasConversion<int>();
            
            entity.Property(el => el.Timestamp)
                .HasColumnName("timestamp")
                .HasDefaultValueSql("NOW()");
            
            entity.Property(el => el.Details)
                .HasColumnName("details");
            
            entity.HasIndex(el => el.QueueSessionId);
            entity.HasIndex(el => el.TicketId);
            entity.HasIndex(el => new { el.QueueSessionId, el.Timestamp })
                .HasName("idx_eventlog_session_time");
        });
        
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(r => r.Id);
            
            entity.Property(r => r.Id)
                .HasColumnName("id");
            
            entity.Property(r => r.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(r => r.Code)
                .HasColumnName("code");
            
            entity.Property(r => r.Description)
                .HasColumnName("description");
            
            entity.Property(r => r.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");
            
            entity.HasIndex(r => r.Name)
                .IsUnique();
        });
        
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(ur => ur.Id);
            
            entity.Property(ur => ur.Id)
                .HasColumnName("id");
            
            entity.Property(ur => ur.UserId)
                .HasColumnName("user_id");
            
            entity.Property(ur => ur.RoleId)
                .HasColumnName("role_id");
            
            entity.Property(ur => ur.AssignedAt)
                .HasColumnName("assigned_at");
            
            entity.Property(ur => ur.AssignedBy)
                .HasColumnName("assigned_by");
            
            entity.HasIndex(ur => ur.UserId);
        });
        
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("user_sessions");
            entity.HasKey(us => us.Id);
            
            entity.Property(us => us.Id)
                .HasColumnName("id");
            
            entity.Property(us => us.UserId)
                .HasColumnName("user_id");
            
            entity.Property(us => us.Token)
                .HasColumnName("token")
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(us => us.IpAddress)
                .HasColumnName("ip_address")
                .HasMaxLength(45);
            
            entity.Property(us => us.UserAgent)
                .HasColumnName("user_agent")
                .HasMaxLength(2048);
            
            entity.Property(us => us.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");
            
            entity.Property(us => us.ExpiresAt)
                .HasColumnName("expires_at");
            
            entity.Property(us => us.LastActivityAt)
                .HasColumnName("last_activity_at");
            
            entity.Property(us => us.IsActive)
                .HasColumnName("is_active");
            
            entity.HasIndex(us => us.UserId);
        });
    }
}
