using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class ControlCloudClientInstallationEntityConfiguration
    : IEntityTypeConfiguration<ControlCloudClientInstallationEntity>
{
    public void Configure(EntityTypeBuilder<ControlCloudClientInstallationEntity> builder)
    {
        builder.ToTable("client_installations");

        builder.HasKey(installation => installation.InstallationId);

        builder.Property(installation => installation.InstallationId)
            .HasColumnName("installation_id")
            .HasMaxLength(160)
            .ValueGeneratedNever();
        builder.Property(installation => installation.ClientId)
            .HasColumnName("client_id")
            .IsRequired();
        builder.Property(installation => installation.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(installation => installation.BootstrapMode)
            .HasColumnName("bootstrap_mode")
            .HasMaxLength(40);
        builder.Property(installation => installation.ClientDeploymentMode)
            .HasColumnName("client_deployment_mode")
            .HasMaxLength(40);
        builder.Property(installation => installation.SiteId)
            .HasColumnName("site_id")
            .HasMaxLength(160);
        builder.Property(installation => installation.SiteRole)
            .HasColumnName("site_role")
            .HasMaxLength(40);
        builder.Property(installation => installation.ParentSiteId)
            .HasColumnName("parent_site_id")
            .HasMaxLength(160);
        builder.Property(installation => installation.BranchCode)
            .HasColumnName("branch_code")
            .HasMaxLength(80);
        builder.Property(installation => installation.SyncTopologyId)
            .HasColumnName("sync_topology_id")
            .HasMaxLength(160);
        builder.Property(installation => installation.RegisteredAtUtc)
            .HasColumnName("registered_at_utc")
            .IsRequired();
        builder.Property(installation => installation.LastBundleIssuedAtUtc)
            .HasColumnName("last_bundle_issued_at_utc");
        builder.Property(installation => installation.LatestEntitlementVersion)
            .HasColumnName("latest_entitlement_version")
            .IsRequired();

        builder.HasIndex(installation => installation.ClientId)
            .HasDatabaseName("ix_client_installations_client_id");
    }
}
