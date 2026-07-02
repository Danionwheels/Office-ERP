using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class ControlCloudClientPortalUserEntityConfiguration
    : IEntityTypeConfiguration<ControlCloudClientPortalUserEntity>
{
    public void Configure(EntityTypeBuilder<ControlCloudClientPortalUserEntity> builder)
    {
        builder.ToTable("client_portal_users");

        builder.HasKey(user => user.UserId);

        builder.Property(user => user.UserId)
            .HasColumnName("user_id")
            .ValueGeneratedNever();
        builder.Property(user => user.ClientId)
            .HasColumnName("client_id")
            .IsRequired();
        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();
        builder.Property(user => user.NormalizedEmail)
            .HasColumnName("normalized_email")
            .HasMaxLength(320)
            .IsRequired();
        builder.Property(user => user.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(180)
            .IsRequired();
        builder.Property(user => user.Role)
            .HasColumnName("role")
            .HasMaxLength(40)
            .IsRequired();
        builder.Property(user => user.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(220)
            .IsRequired();
        builder.Property(user => user.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(user => user.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();
        builder.Property(user => user.LastLoginAtUtc)
            .HasColumnName("last_login_at_utc");

        builder.HasIndex(user => new { user.ClientId, user.NormalizedEmail })
            .IsUnique()
            .HasDatabaseName("ux_client_portal_users_client_email");
        builder.HasIndex(user => new { user.ClientId, user.Status })
            .HasDatabaseName("ix_client_portal_users_client_status");
    }
}
