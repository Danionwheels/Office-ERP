using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class ControlCloudClientPortalSessionEntityConfiguration
    : IEntityTypeConfiguration<ControlCloudClientPortalSessionEntity>
{
    public void Configure(EntityTypeBuilder<ControlCloudClientPortalSessionEntity> builder)
    {
        builder.ToTable("client_portal_sessions");
        builder.HasKey(session => session.SessionId);
        builder.Property(session => session.SessionId).HasColumnName("session_id").ValueGeneratedNever();
        builder.Property(session => session.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(session => session.ClientId).HasColumnName("client_id").IsRequired();
        builder.Property(session => session.Role).HasColumnName("role").HasMaxLength(40).IsRequired();
        builder.Property(session => session.SecurityVersion).HasColumnName("security_version").IsRequired();
        builder.Property(session => session.RefreshTokenHash).HasColumnName("refresh_token_hash").HasMaxLength(128).IsRequired();
        builder.Property(session => session.PreviousRefreshTokenHash).HasColumnName("previous_refresh_token_hash").HasMaxLength(128);
        builder.Property(session => session.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(session => session.LastActivityAtUtc).HasColumnName("last_activity_at_utc").IsRequired();
        builder.Property(session => session.IdleExpiresAtUtc).HasColumnName("idle_expires_at_utc").IsRequired();
        builder.Property(session => session.AbsoluteExpiresAtUtc).HasColumnName("absolute_expires_at_utc").IsRequired();
        builder.Property(session => session.RevokedAtUtc).HasColumnName("revoked_at_utc");
        builder.Property(session => session.RevokedReason).HasColumnName("revoked_reason").HasMaxLength(300);
        builder.Property(session => session.ConcurrencyToken)
            .HasColumnName("concurrency_token")
            .IsConcurrencyToken()
            .IsRequired();
        builder.HasIndex(session => session.RefreshTokenHash).IsUnique()
            .HasDatabaseName("ux_client_portal_sessions_refresh_hash");
        builder.HasIndex(session => session.PreviousRefreshTokenHash)
            .HasDatabaseName("ix_client_portal_sessions_previous_refresh_hash");
        builder.HasIndex(session => new { session.UserId, session.RevokedAtUtc })
            .HasDatabaseName("ix_client_portal_sessions_user_revoked");
        builder.HasIndex(session => session.IdleExpiresAtUtc)
            .HasDatabaseName("ix_client_portal_sessions_idle_expiry");
    }
}
