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

        builder.Property(u => u.Login)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(u => u.Login)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.Email)
            .HasMaxLength(255);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(u => u.UpdatedAt);

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
