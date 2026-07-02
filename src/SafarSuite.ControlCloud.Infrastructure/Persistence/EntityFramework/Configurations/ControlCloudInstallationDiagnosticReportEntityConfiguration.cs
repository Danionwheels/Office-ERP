using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class ControlCloudInstallationDiagnosticReportEntityConfiguration
    : IEntityTypeConfiguration<ControlCloudInstallationDiagnosticReportEntity>
{
    public void Configure(EntityTypeBuilder<ControlCloudInstallationDiagnosticReportEntity> builder)
    {
        builder.ToTable("installation_diagnostic_reports");

        builder.HasKey(report => report.DiagnosticReportId);

        builder.Property(report => report.DiagnosticReportId)
            .HasColumnName("diagnostic_report_id")
            .ValueGeneratedNever();
        builder.Property(report => report.ClientId)
            .HasColumnName("client_id")
            .IsRequired();
        builder.Property(report => report.InstallationId)
            .HasColumnName("installation_id")
            .HasMaxLength(160)
            .IsRequired();
        builder.Property(report => report.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(report => report.ReceivedAtUtc)
            .HasColumnName("received_at_utc")
            .IsRequired();
        builder.Property(report => report.GeneratedAtUtc)
            .HasColumnName("generated_at_utc")
            .IsRequired();
        builder.Property(report => report.UploadedBy)
            .HasColumnName("uploaded_by")
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(report => report.Reason)
            .HasColumnName("reason")
            .HasMaxLength(500)
            .IsRequired();
        builder.Property(report => report.LocalServerVersion)
            .HasColumnName("local_server_version")
            .HasMaxLength(80)
            .IsRequired();
        builder.Property(report => report.LicenseStatus)
            .HasColumnName("license_status")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(report => report.BundleJson)
            .HasColumnName("bundle_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(report => report.ClientId)
            .HasDatabaseName("ix_installation_diagnostic_reports_client_id");
        builder.HasIndex(report => new { report.InstallationId, report.ReceivedAtUtc })
            .HasDatabaseName("ix_installation_diagnostic_reports_installation_received");

        builder.HasOne<ControlCloudClientInstallationEntity>()
            .WithMany()
            .HasForeignKey(report => report.InstallationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
