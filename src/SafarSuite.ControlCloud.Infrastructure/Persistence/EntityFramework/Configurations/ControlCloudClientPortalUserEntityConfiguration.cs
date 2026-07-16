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
        builder.Property(user => user.ProtectedTotpSecret)
            .HasColumnName("protected_totp_secret")
            .HasMaxLength(1024);
        builder.Property(user => user.PendingProtectedTotpSecret)
            .HasColumnName("pending_protected_totp_secret")
            .HasMaxLength(1024);
        builder.Property(user => user.TotpEnrollmentStartedAtUtc)
            .HasColumnName("totp_enrollment_started_at_utc");
        builder.Property(user => user.TotpEnabledAtUtc)
            .HasColumnName("totp_enabled_at_utc");
        builder.Property(user => user.LastTotpStep)
            .HasColumnName("last_totp_step");
        builder.Property(user => user.RecoveryCodeHashesJson)
            .HasColumnName("recovery_code_hashes_json")
            .HasColumnType("text")
            .HasDefaultValue("[]")
            .IsRequired();
        builder.Property(user => user.PendingRecoveryCodeHashesJson)
            .HasColumnName("pending_recovery_code_hashes_json")
            .HasColumnType("text")
            .HasDefaultValue("[]")
            .IsRequired();
        builder.Property(user => user.RecoveryCodesGeneratedAtUtc)
            .HasColumnName("recovery_codes_generated_at_utc");
        builder.Property(user => user.LastRecoveryCodeUsedAtUtc)
            .HasColumnName("last_recovery_code_used_at_utc");
        builder.Property(user => user.SecurityVersion)
            .HasColumnName("security_version")
            .HasDefaultValue(1)
            .IsRequired();
        builder.Property(user => user.ConcurrencyToken)
            .HasColumnName("concurrency_token")
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasIndex(user => new { user.ClientId, user.NormalizedEmail })
            .IsUnique()
            .HasDatabaseName("ux_client_portal_users_client_email");
        builder.HasIndex(user => new { user.ClientId, user.Status })
            .HasDatabaseName("ix_client_portal_users_client_status");
    }
}
