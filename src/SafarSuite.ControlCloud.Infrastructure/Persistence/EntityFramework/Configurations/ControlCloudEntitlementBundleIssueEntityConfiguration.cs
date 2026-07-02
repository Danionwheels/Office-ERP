using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class ControlCloudEntitlementBundleIssueEntityConfiguration
    : IEntityTypeConfiguration<ControlCloudEntitlementBundleIssueEntity>
{
    public void Configure(EntityTypeBuilder<ControlCloudEntitlementBundleIssueEntity> builder)
    {
        builder.ToTable("entitlement_bundle_issues");

        builder.HasKey(issue => issue.BundleIssueId);

        builder.Property(issue => issue.BundleIssueId)
            .HasColumnName("bundle_issue_id")
            .ValueGeneratedNever();
        builder.Property(issue => issue.ClientId)
            .HasColumnName("client_id")
            .IsRequired();
        builder.Property(issue => issue.InstallationId)
            .HasColumnName("installation_id")
            .HasMaxLength(160)
            .IsRequired();
        builder.Property(issue => issue.EntitlementVersion)
            .HasColumnName("entitlement_version")
            .IsRequired();
        builder.Property(issue => issue.EntitlementSnapshotId)
            .HasColumnName("entitlement_snapshot_id")
            .IsRequired();
        builder.Property(issue => issue.IssuedAtUtc)
            .HasColumnName("issued_at_utc")
            .IsRequired();
        builder.Property(issue => issue.Algorithm)
            .HasColumnName("algorithm")
            .HasMaxLength(40)
            .IsRequired();
        builder.Property(issue => issue.KeyId)
            .HasColumnName("key_id")
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(issue => issue.PayloadSha256)
            .HasColumnName("payload_sha256")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(issue => issue.SignatureValue)
            .HasColumnName("signature_value")
            .HasMaxLength(512)
            .IsRequired();
        builder.Property(issue => issue.PayloadJson)
            .HasColumnName("payload_json")
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(issue => issue.PaidUntil)
            .HasColumnName("paid_until")
            .IsRequired();
        builder.Property(issue => issue.WarningStartsAt)
            .HasColumnName("warning_starts_at")
            .IsRequired();
        builder.Property(issue => issue.GraceUntil)
            .HasColumnName("grace_until")
            .IsRequired();
        builder.Property(issue => issue.OfflineValidUntil)
            .HasColumnName("offline_valid_until")
            .IsRequired();

        builder.HasIndex(issue => issue.ClientId)
            .HasDatabaseName("ix_entitlement_bundle_issues_client_id");
        builder.HasIndex(issue => issue.InstallationId)
            .HasDatabaseName("ix_entitlement_bundle_issues_installation_id");
        builder.HasIndex(issue => new { issue.InstallationId, issue.EntitlementVersion })
            .HasDatabaseName("ix_entitlement_bundle_issues_installation_version");

        builder.HasOne<ControlCloudClientInstallationEntity>()
            .WithMany()
            .HasForeignKey(issue => issue.InstallationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
