using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Configurations;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;

public sealed class ControlCloudDbContext : DbContext
{
    public ControlCloudDbContext(DbContextOptions<ControlCloudDbContext> options)
        : base(options)
    {
    }

    public DbSet<ControlDeskEnvelopeReceiptEntity> ControlDeskEnvelopeReceipts =>
        Set<ControlDeskEnvelopeReceiptEntity>();

    public DbSet<ControlCloudClientCommercialProjectionEntity> ClientCommercialProjections =>
        Set<ControlCloudClientCommercialProjectionEntity>();

    public DbSet<ControlCloudClientPortalInvitationEntity> ClientPortalInvitations =>
        Set<ControlCloudClientPortalInvitationEntity>();

    public DbSet<ControlCloudClientPortalUserEntity> ClientPortalUsers =>
        Set<ControlCloudClientPortalUserEntity>();

    public DbSet<ControlCloudClientInstallationEntity> ClientInstallations =>
        Set<ControlCloudClientInstallationEntity>();

    public DbSet<ControlCloudEntitlementBundleIssueEntity> EntitlementBundleIssues =>
        Set<ControlCloudEntitlementBundleIssueEntity>();

    public DbSet<ControlCloudInstallationCommandEntity> InstallationCommands =>
        Set<ControlCloudInstallationCommandEntity>();

    public DbSet<ControlCloudInstallationSetupTokenEntity> InstallationSetupTokens =>
        Set<ControlCloudInstallationSetupTokenEntity>();

    public DbSet<ControlCloudInstallationCommandAcknowledgementEntity> InstallationCommandAcknowledgements =>
        Set<ControlCloudInstallationCommandAcknowledgementEntity>();

    public DbSet<ControlCloudInstallationHeartbeatEntity> InstallationHeartbeats =>
        Set<ControlCloudInstallationHeartbeatEntity>();

    public DbSet<ControlCloudInstallationDiagnosticReportEntity> InstallationDiagnosticReports =>
        Set<ControlCloudInstallationDiagnosticReportEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("cloud");
        modelBuilder.ApplyConfiguration(new ControlDeskEnvelopeReceiptEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudClientCommercialProjectionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudClientPortalInvitationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudClientPortalUserEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudClientInstallationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudEntitlementBundleIssueEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudInstallationCommandEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudInstallationSetupTokenEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudInstallationCommandAcknowledgementEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudInstallationHeartbeatEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudInstallationDiagnosticReportEntityConfiguration());
    }
}
