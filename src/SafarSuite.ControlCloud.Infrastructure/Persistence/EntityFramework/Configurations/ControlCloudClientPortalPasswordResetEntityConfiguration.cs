using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class ControlCloudClientPortalPasswordResetEntityConfiguration
    : IEntityTypeConfiguration<ControlCloudClientPortalPasswordResetEntity>
{
    public void Configure(EntityTypeBuilder<ControlCloudClientPortalPasswordResetEntity> builder)
    {
        builder.ToTable("client_portal_password_resets");
        builder.HasKey(reset => reset.PasswordResetId);
        builder.Property(reset => reset.PasswordResetId).HasColumnName("password_reset_id").ValueGeneratedNever();
        builder.Property(reset => reset.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(reset => reset.ClientId).HasColumnName("client_id").IsRequired();
        builder.Property(reset => reset.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
        builder.Property(reset => reset.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(reset => reset.ExpiresAtUtc).HasColumnName("expires_at_utc").IsRequired();
        builder.Property(reset => reset.UsedAtUtc).HasColumnName("used_at_utc");
        builder.Property(reset => reset.ConcurrencyToken)
            .HasColumnName("concurrency_token")
            .IsConcurrencyToken()
            .IsRequired();
        builder.HasIndex(reset => reset.TokenHash).IsUnique()
            .HasDatabaseName("ux_client_portal_password_resets_token_hash");
        builder.HasIndex(reset => new { reset.UserId, reset.ExpiresAtUtc })
            .HasDatabaseName("ix_client_portal_password_resets_user_expiry");
    }
}
