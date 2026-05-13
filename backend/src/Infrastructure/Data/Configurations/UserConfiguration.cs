using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id");

        builder.Property(u => u.Login)
            .HasColumnName("login")
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(u => u.Login)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.FullName)
            .HasColumnName("full_name")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.LastName)
            .HasColumnName("last_name")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(255);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at");

        // Navigation: UserRoles
        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation: UserSessions
        builder.HasMany(u => u.UserSessions)
            .WithOne(us => us.User)
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation: CreatedQueueConfigs
        builder.HasMany(u => u.CreatedQueueConfigs)
            .WithOne(qc => qc.CreatedBy)
            .HasForeignKey(qc => qc.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation: CreatedQueueSessions
        builder.HasMany(u => u.CreatedQueueSessions)
            .WithOne(qs => qs.CreatedBy)
            .HasForeignKey(qs => qs.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation: ServedTickets
        builder.HasMany(u => u.ServedTickets)
            .WithOne(t => t.ServedByUser)
            .HasForeignKey(t => t.ServedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Navigation: EventLogs
        builder.HasMany(u => u.EventLogs)
            .WithOne(el => el.ActorUser)
            .HasForeignKey(el => el.ActorUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
