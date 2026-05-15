using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data.Converters;

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
        
        // EF Core configurations disabled - using SQL scripts for schema management
        // modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        // Configure User entity with unique email index
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            
            entity.Property(u => u.Id)
                .HasColumnName("id");
            
            entity.Property(u => u.Login)
                .HasColumnName("login")
                .IsRequired()
                .HasMaxLength(100);
            
            entity.HasIndex(u => u.Login)
                .IsUnique()
                .HasName("idx_user_login");
            
            entity.Property(u => u.PasswordHash)
                .HasColumnName("password_hash")
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(u => u.FullName)
                .HasColumnName("full_name")
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(u => u.LastName)
                .HasColumnName("last_name")
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(u => u.Email)
                .HasColumnName("email")
                .HasMaxLength(255);
            
            entity.HasIndex(u => u.Email)
                .IsUnique();
            
            entity.Property(u => u.IsActive)
                .HasColumnName("is_active")
                .IsRequired()
                .HasDefaultValue(true);
            
            entity.Property(u => u.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("NOW()")
                .HasConversion(typeof(DateTimeUtcConverter));
            
            entity.Property(u => u.UpdatedAt)
                .HasColumnName("updated_at")
                .HasConversion(typeof(NullableDateTimeUtcConverter));
        });
        
        // Configure Role entity
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(r => r.Id);
            
            entity.Property(r => r.Id)
                .HasColumnName("id");
            
            entity.Property(r => r.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(r => r.Code)
                .HasColumnName("code")
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(r => r.Description)
                .HasColumnName("description");
            
            entity.Property(r => r.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()")
                .HasConversion(typeof(DateTimeUtcConverter));
            
            entity.HasIndex(r => r.Code)
                .IsUnique()
                .HasName("idx_roles_code");
        });
        
        // Configure UserRole entity
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(ur => ur.Id);
            
            entity.Property(ur => ur.Id)
                .HasColumnName("id");
            
            entity.Property(ur => ur.UserId)
                .HasColumnName("user_id")
                .IsRequired();
            
            entity.Property(ur => ur.RoleId)
                .HasColumnName("role_id")
                .IsRequired();
            
            entity.Property(ur => ur.AssignedAt)
                .HasColumnName("assigned_at")
                .HasDefaultValueSql("NOW()")
                .HasConversion(typeof(DateTimeUtcConverter));
            
            entity.Property(ur => ur.AssignedBy)
                .HasColumnName("assigned_by");
            
            // Configure explicit relationships
            entity.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .HasConstraintName("fk_user_roles_users_user_id")
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .HasConstraintName("fk_user_roles_roles_role_id")
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(ur => ur.AssignedByUser)
                .WithMany()
                .HasForeignKey(ur => ur.AssignedBy)
                .HasConstraintName("fk_user_roles_assigned_by")
                .OnDelete(DeleteBehavior.SetNull);
        });
        
        // Configure UserSession entity
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("user_sessions");
            entity.HasKey(us => us.Id);
            
            entity.Property(us => us.Id)
                .HasColumnName("id");
            
            entity.Property(us => us.UserId)
                .HasColumnName("user_id")
                .IsRequired();
            
            entity.Property(us => us.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()")
                .HasConversion(typeof(DateTimeUtcConverter));
            
            entity.Property(us => us.ExpiresAt)
                .HasColumnName("expires_at")
                .HasConversion(typeof(NullableDateTimeUtcConverter));
            
            entity.Property(us => us.Token)
                .HasColumnName("token")
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(us => us.IpAddress)
                .HasColumnName("ip_address")
                .HasMaxLength(45);
            
            entity.Property(us => us.UserAgent)
                .HasColumnName("user_agent")
                .HasMaxLength(500);
            
            entity.Property(us => us.LastActivityAt)
                .HasColumnName("last_activity_at")
                .HasDefaultValueSql("NOW()")
                .HasConversion(typeof(DateTimeUtcConverter));
            
            entity.Property(us => us.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);
            
            // Configure explicit relationship
            entity.HasOne(us => us.User)
                .WithMany(u => u.UserSessions)
                .HasForeignKey(us => us.UserId)
                .HasConstraintName("fk_user_sessions_users_user_id")
                .OnDelete(DeleteBehavior.Cascade);
        });
        
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
                .HasConversion(new DistributionModeToStringConverter())
                .HasMaxLength(20);
            
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
                .HasDefaultValueSql("NOW()")
                .HasConversion(typeof(DateTimeUtcConverter));
            
            entity.Property(qc => qc.CreatedById)
                .HasColumnName("created_by_id");
            
            // Check constraint: priority_escalation_wait_min >= 0
            entity.HasCheckConstraint("chk_queue_configs_priority_wait",
                "CHECK (priority_escalation_wait_min IS NULL OR priority_escalation_wait_min >= 0)");
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
                .HasConversion(new SessionStatusToStringConverter())
                .HasMaxLength(20);
            
            entity.Property(qs => qs.StartedAt)
                .HasColumnName("started_at")
                .HasConversion(typeof(NullableDateTimeUtcConverter));
            
            entity.Property(qs => qs.ClosedAt)
                .HasColumnName("closed_at")
                .HasConversion(typeof(NullableDateTimeUtcConverter));
            
            entity.Property(qs => qs.CreatedById)
                .HasColumnName("created_by");
            
            entity.Property(qs => qs.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");
            
            entity.HasIndex(qs => qs.QueueConfigId);
            
            // Check constraint: closed_at >= started_at
            entity.HasCheckConstraint("chk_queue_sessions_dates",
                "CHECK (closed_at IS NULL OR started_at IS NULL OR closed_at >= started_at)");

            // Index for queue status
            entity.HasIndex(qs => new { qs.QueueConfigId, qs.Status })
                .HasName("idx_session_queue_status");
            
            // Unique constraint: only one OPEN session per config
            entity.HasIndex(qs => new { qs.QueueConfigId, qs.Status })
                .HasName("uq_queue_sessions_one_open_per_config")
                .IsUnique()
                .HasFilter("WHERE status = 'OPEN'");
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
                .HasConversion(new TicketStatusToStringConverter())
                .HasMaxLength(20);
            
            entity.Property(t => t.Version)
                .HasColumnName("version")
                .HasDefaultValue(1);
            
            entity.Property(t => t.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()")
                .HasConversion(typeof(DateTimeUtcConverter));
            
            entity.Property(t => t.CalledAt)
                .HasColumnName("called_at")
                .HasConversion(typeof(NullableDateTimeUtcConverter));
            
            entity.Property(t => t.ServiceStartedAt)
                .HasColumnName("service_started_at")
                .HasConversion(typeof(NullableDateTimeUtcConverter));
            
            entity.Property(t => t.ServiceEndedAt)
                .HasColumnName("service_ended_at")
                .HasConversion(typeof(NullableDateTimeUtcConverter));
            
            entity.Property(t => t.UpdatedAt)
                .HasColumnName("updated_at")
                .HasConversion(typeof(NullableDateTimeUtcConverter));
            
            entity.Property(t => t.ServedByUserId)
                .HasColumnName("served_by_user_id");
            
            entity.Property(t => t.ClientSessionId)
                .HasColumnName("client_session_id");
            
            entity.Property(t => t.CancelReason)
                .HasColumnName("cancel_reason");
            
            // Check constraints
            entity.HasCheckConstraint("chk_tickets_sort_order",
                "CHECK (sort_order >= 0)");
            entity.HasCheckConstraint("chk_tickets_priority_level",
                "CHECK (priority_level >= 0)");
            entity.HasCheckConstraint("chk_tickets_version",
                "CHECK (version >= 1)");
            entity.HasCheckConstraint("chk_tickets_called_at",
                "CHECK (called_at IS NULL OR called_at >= created_at)");
            entity.HasCheckConstraint("chk_tickets_service_started_at",
                "CHECK (service_started_at IS NULL OR service_started_at >= created_at)");
            entity.HasCheckConstraint("chk_tickets_service_ended_at",
                "CHECK (service_ended_at IS NULL OR service_started_at IS NULL OR service_ended_at >= service_started_at)");
            entity.HasCheckConstraint("chk_tickets_served_requires_end_time",
                "CHECK ((status = 'SERVED' AND service_ended_at IS NOT NULL) OR status <> 'SERVED')");
            entity.HasCheckConstraint("chk_tickets_skipped_cancelled_require_end_time",
                "CHECK ((status IN ('SKIPPED', 'CANCELLED') AND service_ended_at IS NOT NULL) OR status NOT IN ('SKIPPED', 'CANCELLED'))");

            // Index for queue display sorting
            entity.HasIndex(t => new { t.QueueSessionId, t.Status, t.PriorityLevel, t.SortOrder, t.CreatedAt })
                .HasName("idx_ticket_queue_sort");
            
            // Index: client session lookup
            entity.HasIndex(t => new { t.ClientSessionId, t.Status })
                .HasName("idx_ticket_client_session");
            
            // Index: status + time lookup
            entity.HasIndex(t => new { t.QueueSessionId, t.Status, t.CreatedAt })
                .HasName("idx_ticket_status_time");
            
            // Index: service type lookup
            entity.HasIndex(t => new { t.QueueSessionId, t.ServiceTypeId, t.Status })
                .HasName("idx_ticket_service_type");
            
            // Index: served tickets aggregation (filtered)
            entity.HasIndex(t => new { t.QueueSessionId, t.Status })
                .HasName("idx_ticket_served_agg")
                .HasFilter("WHERE status = 'SERVED'");
            
            // Unique constraint: only one active ticket per client session
            entity.HasIndex(t => new { t.ClientSessionId, t.Status })
                .HasName("uq_tickets_one_active_per_client_session")
                .IsUnique()
                .HasFilter("WHERE client_session_id IS NOT NULL AND status IN ('WAITING', 'CALLED')");
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
                .HasDefaultValueSql("NOW()")
                .HasConversion(typeof(DateTimeUtcConverter));
            
            // Check constraints
            entity.HasCheckConstraint("chk_service_types_priority",
                "CHECK (base_priority_level >= 0)");
            entity.HasCheckConstraint("chk_service_types_plan_time",
                "CHECK (plan_avg_service_time_sec IS NULL OR plan_avg_service_time_sec > 0)");
            
            entity.HasIndex(st => st.QueueConfigId);
            
            // Index: service types by queue, active, sort order
            entity.HasIndex(st => new { st.QueueConfigId, st.IsActive, st.SortOrder })
                .HasName("idx_servicetype_queue");
            
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
                .HasDefaultValueSql("NOW()")
                .HasConversion(typeof(DateTimeUtcConverter));
            
            entity.HasIndex(es => es.QueueSessionId);
            
            // Check constraint: NOT (is_ready = TRUE AND current_ticket_id IS NOT NULL)
            entity.HasCheckConstraint("chk_executor_states_ready_only_without_ticket",
                "CHECK (NOT (is_ready = TRUE AND current_ticket_id IS NOT NULL))");

            // Ready Executors Index (filtered)
            entity.HasIndex(es => new { es.QueueSessionId, es.IsReady })
                .HasName("idx_executor_ready")
                .HasFilter("WHERE is_ready = TRUE");
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
                .HasDefaultValueSql("NOW()")
                .HasConversion(typeof(DateTimeUtcConverter));
            
            entity.Property(cs => cs.ExpiresAt)
                .HasColumnName("expires_at")
                .HasConversion(typeof(NullableDateTimeUtcConverter));
            
            entity.Property(cs => cs.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);
            
            entity.Property(cs => cs.IpAddress)
                .HasColumnName("ip_address");
            
            entity.Property(cs => cs.UserAgent)
                .HasColumnName("user_agent");
            
            entity.HasIndex(cs => cs.DeviceFingerprint);
            
            // Index: active client sessions
            entity.HasIndex(cs => new { cs.DeviceFingerprint, cs.IsActive })
                .HasName("idx_clientsession_active");
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
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(el => el.EventType)
                .HasName("idx_eventlog_type");
            
            entity.Property(el => el.Timestamp)
                .HasColumnName("timestamp")
                .HasDefaultValueSql("NOW()")
                .HasConversion(typeof(DateTimeUtcConverter));
            
            entity.Property(el => el.Details)
                .HasColumnName("details")
                .HasColumnType("jsonb");
            
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
                .HasMaxLength(100);
            
            entity.Property(r => r.Code)
                .HasColumnName("code")
                .IsRequired()
                .HasMaxLength(50);
            
            entity.HasIndex(r => r.Code)
                .IsUnique()
                .HasName("idx_roles_code");
            
            entity.Property(r => r.Description)
                .HasColumnName("description");
            
            entity.Property(r => r.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");
            
            entity.HasIndex(r => r.Name)
                .IsUnique();
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
