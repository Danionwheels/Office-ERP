using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class ControlCloudEntitlementBundleIssueEntityConfiguration
    : IEntityTypeConfiguration<ControlCloudEntitlementBundleIssueEntity>
{
    public void Configure(EntityTypeBuilder<ControlCloudEntitlementBundleIssueEntity> builder)
    {
        builder.ToTable("entitlement_bundle_issues", table =>
        {
            table.HasCheckConstraint(
                "ck_entitlement_bundle_issues_named_users",
                "allowed_named_users IS NULL OR allowed_named_users >= 0");
            table.HasCheckConstraint(
                "ck_entitlement_bundle_issues_concurrent_users",
                "allowed_concurrent_users IS NULL OR allowed_concurrent_users >= 0");
            table.HasCheckConstraint(
                "ck_entitlement_bundle_issues_user_limit_order",
                "allowed_named_users IS NULL OR allowed_concurrent_users IS NULL OR allowed_concurrent_users <= allowed_named_users");
            table.HasCheckConstraint(
                "ck_entitlement_bundle_issues_feature_limit_count",
                "feature_limit_count >= 0");
        });

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
        builder.Property(issue => issue.ClientAccessRevisionId)
            .HasColumnName("client_access_revision_id")
            .IsRequired();
        builder.Property(issue => issue.ContractRevisionNumber)
            .HasColumnName("contract_revision_number")
            .IsRequired();
        builder.Property(issue => issue.ProductCatalogRevisionId)
            .HasColumnName("product_catalog_revision_id")
            .IsRequired();
        builder.Property(issue => issue.ProductCatalogRevisionNumber)
            .HasColumnName("product_catalog_revision_number")
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
        builder.Property(issue => issue.AllowedNamedUsers)
            .HasColumnName("allowed_named_users");
        builder.Property(issue => issue.AllowedConcurrentUsers)
            .HasColumnName("allowed_concurrent_users");
        builder.Property(issue => issue.FeatureLimitCount)
            .HasColumnName("feature_limit_count")
            .IsRequired();

        builder.HasIndex(issue => issue.ClientId)
            .HasDatabaseName("ix_entitlement_bundle_issues_client_id");
        builder.HasIndex(issue => issue.InstallationId)
            .HasDatabaseName("ix_entitlement_bundle_issues_installation_id");
        builder.HasIndex(issue => new { issue.InstallationId, issue.EntitlementVersion })
            .HasDatabaseName("ix_entitlement_bundle_issues_installation_version");
        builder.HasIndex(issue => issue.ClientAccessRevisionId)
            .HasDatabaseName("ix_entitlement_bundle_issues_access_revision");
        builder.HasIndex(issue => issue.ProductCatalogRevisionId)
            .HasDatabaseName("ix_entitlement_bundle_issues_product_catalog_revision");

        builder.HasOne<ControlCloudClientInstallationEntity>()
            .WithMany()
            .HasForeignKey(issue => issue.InstallationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
