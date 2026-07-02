using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class ControlCloudInstallationSetupTokenEntityConfiguration
    : IEntityTypeConfiguration<ControlCloudInstallationSetupTokenEntity>
{
    public void Configure(EntityTypeBuilder<ControlCloudInstallationSetupTokenEntity> builder)
    {
        builder.ToTable("installation_setup_tokens");

        builder.HasKey(setupToken => setupToken.SetupTokenId);

        builder.Property(setupToken => setupToken.SetupTokenId)
            .HasColumnName("setup_token_id")
            .ValueGeneratedNever();
        builder.Property(setupToken => setupToken.ClientId)
            .HasColumnName("client_id")
            .IsRequired();
        builder.Property(setupToken => setupToken.InstallationId)
            .HasColumnName("installation_id")
            .HasMaxLength(160)
            .IsRequired();
        builder.Property(setupToken => setupToken.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(128)
            .IsRequired();
        builder.Property(setupToken => setupToken.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(setupToken => setupToken.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(setupToken => setupToken.DeploymentMode)
            .HasColumnName("deployment_mode")
            .HasMaxLength(40)
            .IsRequired();
        builder.Property(setupToken => setupToken.ClientDeploymentMode)
            .HasColumnName("client_deployment_mode")
            .HasMaxLength(40);
        builder.Property(setupToken => setupToken.SiteId)
            .HasColumnName("site_id")
            .HasMaxLength(160);
        builder.Property(setupToken => setupToken.SiteRole)
            .HasColumnName("site_role")
            .HasMaxLength(40);
        builder.Property(setupToken => setupToken.ParentSiteId)
            .HasColumnName("parent_site_id")
            .HasMaxLength(160);
        builder.Property(setupToken => setupToken.BranchCode)
            .HasColumnName("branch_code")
            .HasMaxLength(80);
        builder.Property(setupToken => setupToken.SyncTopologyId)
            .HasColumnName("sync_topology_id")
            .HasMaxLength(160);
        builder.Property(setupToken => setupToken.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();
        builder.Property(setupToken => setupToken.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .IsRequired();
        builder.Property(setupToken => setupToken.ConsumedAtUtc)
            .HasColumnName("consumed_at_utc");
        builder.Property(setupToken => setupToken.ConsumedLocalServerVersion)
            .HasColumnName("consumed_local_server_version")
            .HasMaxLength(80);

        builder.HasIndex(setupToken => setupToken.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_installation_setup_tokens_token_hash");
        builder.HasIndex(setupToken => new { setupToken.ClientId, setupToken.InstallationId, setupToken.Status })
            .HasDatabaseName("ix_installation_setup_tokens_client_installation_status");
    }
}
