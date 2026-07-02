using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class ClientDeploymentConfiguration : IEntityTypeConfiguration<ClientDeployment>
{
    public void Configure(EntityTypeBuilder<ClientDeployment> builder)
    {
        builder.ToTable("client_deployments");

        builder.HasKey(deployment => deployment.Id);

        builder.Property(deployment => deployment.Id)
            .HasColumnName("client_deployment_id")
            .HasConversion(
                id => id.Value,
                value => ClientDeploymentId.Create(value))
            .ValueGeneratedNever();

        builder.Property(deployment => deployment.ClientId)
            .HasColumnName("client_id")
            .HasConversion(
                id => id.Value,
                value => ClientId.Create(value))
            .IsRequired();

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(deployment => deployment.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(deployment => deployment.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(deployment => deployment.InstallationId)
            .HasColumnName("installation_id")
            .HasMaxLength(160)
            .IsRequired();

        builder.HasIndex(deployment => new { deployment.ClientId, deployment.InstallationId })
            .IsUnique()
            .HasDatabaseName("ux_client_deployments_client_installation");

        builder.Property(deployment => deployment.BootstrapMode)
            .HasColumnName("bootstrap_mode")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(deployment => deployment.ClientDeploymentMode)
            .HasColumnName("client_deployment_mode")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(deployment => deployment.SiteId)
            .HasColumnName("site_id")
            .HasMaxLength(96)
            .IsRequired();

        builder.Property(deployment => deployment.SiteRole)
            .HasColumnName("site_role")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(deployment => deployment.ParentSiteId)
            .HasColumnName("parent_site_id")
            .HasMaxLength(96);

        builder.Property(deployment => deployment.BranchCode)
            .HasColumnName("branch_code")
            .HasMaxLength(64);

        builder.Property(deployment => deployment.SyncTopologyId)
            .HasColumnName("sync_topology_id")
            .HasMaxLength(96);

        builder.Property(deployment => deployment.LocalServerVersion)
            .HasColumnName("local_server_version")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(deployment => deployment.SafarSuiteAppVersion)
            .HasColumnName("safarsuite_app_version")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(deployment => deployment.IsPrimary)
            .HasColumnName("is_primary")
            .IsRequired();

        builder.HasIndex(deployment => new { deployment.ClientId, deployment.IsPrimary })
            .HasDatabaseName("ix_client_deployments_client_primary");

        builder.Property(deployment => deployment.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(deployment => deployment.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();
    }
}
