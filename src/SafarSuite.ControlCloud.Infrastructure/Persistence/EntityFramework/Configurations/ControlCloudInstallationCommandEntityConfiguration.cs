using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class ControlCloudInstallationCommandEntityConfiguration
    : IEntityTypeConfiguration<ControlCloudInstallationCommandEntity>
{
    public void Configure(EntityTypeBuilder<ControlCloudInstallationCommandEntity> builder)
    {
        builder.ToTable("installation_commands");

        builder.HasKey(command => command.CommandId);

        builder.Property(command => command.CommandId)
            .HasColumnName("command_id")
            .ValueGeneratedNever();
        builder.Property(command => command.ClientId)
            .HasColumnName("client_id")
            .IsRequired();
        builder.Property(command => command.InstallationId)
            .HasColumnName("installation_id")
            .HasMaxLength(160)
            .IsRequired();
        builder.Property(command => command.CommandVersion)
            .HasColumnName("command_version")
            .IsRequired();
        builder.Property(command => command.CommandType)
            .HasColumnName("command_type")
            .HasMaxLength(80)
            .IsRequired();
        builder.Property(command => command.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(command => command.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(240)
            .IsRequired();
        builder.Property(command => command.PayloadJson)
            .HasColumnName("payload_json")
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(command => command.SignatureAlgorithm)
            .HasColumnName("signature_algorithm")
            .HasMaxLength(40)
            .IsRequired();
        builder.Property(command => command.SignatureKeyId)
            .HasColumnName("signature_key_id")
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(command => command.PayloadSha256)
            .HasColumnName("payload_sha256")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(command => command.SignatureValue)
            .HasColumnName("signature_value")
            .HasMaxLength(512)
            .IsRequired();
        builder.Property(command => command.QueuedAtUtc)
            .HasColumnName("queued_at_utc")
            .IsRequired();
        builder.Property(command => command.NotBeforeUtc)
            .HasColumnName("not_before_utc");
        builder.Property(command => command.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .IsRequired();
        builder.Property(command => command.AcknowledgedAtUtc)
            .HasColumnName("acknowledged_at_utc");
        builder.Property(command => command.AcknowledgementStatus)
            .HasColumnName("acknowledgement_status")
            .HasMaxLength(32);
        builder.Property(command => command.AcknowledgementDetail)
            .HasColumnName("acknowledgement_detail")
            .HasMaxLength(1000);

        builder.HasIndex(command => command.ClientId)
            .HasDatabaseName("ix_installation_commands_client_id");
        builder.HasIndex(command => new { command.InstallationId, command.Status })
            .HasDatabaseName("ix_installation_commands_installation_status");
        builder.HasIndex(command => new { command.InstallationId, command.CommandVersion })
            .IsUnique()
            .HasDatabaseName("ux_installation_commands_installation_version");
        builder.HasIndex(command => command.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("ux_installation_commands_idempotency_key");

        builder.HasOne<ControlCloudClientInstallationEntity>()
            .WithMany()
            .HasForeignKey(command => command.InstallationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
