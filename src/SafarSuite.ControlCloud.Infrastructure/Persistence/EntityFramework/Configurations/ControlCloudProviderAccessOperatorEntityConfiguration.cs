using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class ControlCloudProviderAccessOperatorEntityConfiguration
    : IEntityTypeConfiguration<ControlCloudProviderAccessOperatorEntity>
{
    public void Configure(EntityTypeBuilder<ControlCloudProviderAccessOperatorEntity> builder)
    {
        builder.ToTable("provider_access_operators");

        builder.HasKey(providerOperator => providerOperator.UserId);

        builder.Property(providerOperator => providerOperator.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(120)
            .ValueGeneratedNever();
        builder.Property(providerOperator => providerOperator.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();
        builder.Property(providerOperator => providerOperator.NormalizedEmail)
            .HasColumnName("normalized_email")
            .HasMaxLength(320)
            .IsRequired();
        builder.Property(providerOperator => providerOperator.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(180)
            .IsRequired();
        builder.Property(providerOperator => providerOperator.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(220)
            .IsRequired();
        builder.Property(providerOperator => providerOperator.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(providerOperator => providerOperator.ScopesJson)
            .HasColumnName("scopes_json")
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(providerOperator => providerOperator.RecoveryCodeHashesJson)
            .HasColumnName("recovery_code_hashes_json")
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(providerOperator => providerOperator.RecoveryCodesUpdatedAtUtc)
            .HasColumnName("recovery_codes_updated_at_utc");
        builder.Property(providerOperator => providerOperator.RecoveryCodesUpdatedBy)
            .HasColumnName("recovery_codes_updated_by")
            .HasMaxLength(120);
        builder.Property(providerOperator => providerOperator.LastRecoveryCodeUsedAtUtc)
            .HasColumnName("last_recovery_code_used_at_utc");
        builder.Property(providerOperator => providerOperator.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();
        builder.Property(providerOperator => providerOperator.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(providerOperator => providerOperator.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");
        builder.Property(providerOperator => providerOperator.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(120);
        builder.Property(providerOperator => providerOperator.LastLoginAtUtc)
            .HasColumnName("last_login_at_utc");

        builder.HasIndex(providerOperator => providerOperator.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("ux_provider_access_operators_email");
        builder.HasIndex(providerOperator => providerOperator.Status)
            .HasDatabaseName("ix_provider_access_operators_status");
    }
}
