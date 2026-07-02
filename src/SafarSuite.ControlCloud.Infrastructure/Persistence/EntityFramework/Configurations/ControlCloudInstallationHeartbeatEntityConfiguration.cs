using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class ControlCloudInstallationHeartbeatEntityConfiguration
    : IEntityTypeConfiguration<ControlCloudInstallationHeartbeatEntity>
{
    public void Configure(EntityTypeBuilder<ControlCloudInstallationHeartbeatEntity> builder)
    {
        builder.ToTable("installation_heartbeats");

        builder.HasKey(heartbeat => heartbeat.HeartbeatId);

        builder.Property(heartbeat => heartbeat.HeartbeatId)
            .HasColumnName("heartbeat_id")
            .ValueGeneratedNever();
        builder.Property(heartbeat => heartbeat.ClientId)
            .HasColumnName("client_id")
            .IsRequired();
        builder.Property(heartbeat => heartbeat.InstallationId)
            .HasColumnName("installation_id")
            .HasMaxLength(160)
            .IsRequired();
        builder.Property(heartbeat => heartbeat.HeartbeatStatus)
            .HasColumnName("heartbeat_status")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(heartbeat => heartbeat.ReceivedAtUtc)
            .HasColumnName("received_at_utc")
            .IsRequired();
        builder.Property(heartbeat => heartbeat.ReportedAtUtc)
            .HasColumnName("reported_at_utc")
            .IsRequired();
        builder.Property(heartbeat => heartbeat.LicenseStatus)
            .HasColumnName("license_status")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(heartbeat => heartbeat.EntitlementVersion)
            .HasColumnName("entitlement_version");
        builder.Property(heartbeat => heartbeat.PaidUntil)
            .HasColumnName("paid_until");
        builder.Property(heartbeat => heartbeat.WarningStartsAt)
            .HasColumnName("warning_starts_at");
        builder.Property(heartbeat => heartbeat.GraceUntil)
            .HasColumnName("grace_until");
        builder.Property(heartbeat => heartbeat.OfflineValidUntil)
            .HasColumnName("offline_valid_until");
        builder.Property(heartbeat => heartbeat.LocalServerVersion)
            .HasColumnName("local_server_version")
            .HasMaxLength(80);
        builder.Property(heartbeat => heartbeat.Detail)
            .HasColumnName("detail")
            .HasMaxLength(1000);

        builder.HasIndex(heartbeat => heartbeat.ClientId)
            .HasDatabaseName("ix_installation_heartbeats_client_id");
        builder.HasIndex(heartbeat => new { heartbeat.InstallationId, heartbeat.ReceivedAtUtc })
            .HasDatabaseName("ix_installation_heartbeats_installation_received_at");
        builder.HasIndex(heartbeat => new { heartbeat.InstallationId, heartbeat.LicenseStatus })
            .HasDatabaseName("ix_installation_heartbeats_installation_license_status");

        builder.HasOne<ControlCloudClientInstallationEntity>()
            .WithMany()
            .HasForeignKey(heartbeat => heartbeat.InstallationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
