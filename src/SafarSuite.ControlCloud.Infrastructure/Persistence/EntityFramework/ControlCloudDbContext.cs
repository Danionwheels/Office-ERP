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

    public DbSet<ControlCloudCommercialDocumentEntity> CommercialDocuments =>
        Set<ControlCloudCommercialDocumentEntity>();

    public DbSet<ControlCloudClientPortalInvitationEntity> ClientPortalInvitations =>
        Set<ControlCloudClientPortalInvitationEntity>();

    public DbSet<ControlCloudClientPortalMailDeliveryEntity> ClientPortalMailDeliveries =>
        Set<ControlCloudClientPortalMailDeliveryEntity>();

    public DbSet<ControlCloudClientPortalUserEntity> ClientPortalUsers =>
        Set<ControlCloudClientPortalUserEntity>();

    public DbSet<ControlCloudClientPortalSessionEntity> ClientPortalSessions =>
        Set<ControlCloudClientPortalSessionEntity>();

    public DbSet<ControlCloudClientPortalPasswordResetEntity> ClientPortalPasswordResets =>
        Set<ControlCloudClientPortalPasswordResetEntity>();

    public DbSet<ControlCloudClientPortalPaymentClaimEntity> ClientPortalPaymentClaims =>
        Set<ControlCloudClientPortalPaymentClaimEntity>();

    public DbSet<ControlCloudClientPortalAttachmentEntity> ClientPortalAttachments =>
        Set<ControlCloudClientPortalAttachmentEntity>();

    public DbSet<ControlCloudProviderBankDetailsEntity> ProviderBankDetails =>
        Set<ControlCloudProviderBankDetailsEntity>();

    public DbSet<ControlCloudProviderAccessOperatorEntity> ProviderAccessOperators =>
        Set<ControlCloudProviderAccessOperatorEntity>();

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
        modelBuilder.ApplyConfiguration(new ControlCloudCommercialDocumentEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudClientPortalInvitationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudClientPortalMailDeliveryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudClientPortalUserEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudClientPortalSessionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudClientPortalPasswordResetEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudClientPortalPaymentClaimEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudClientPortalAttachmentEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudProviderBankDetailsEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudProviderAccessOperatorEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudClientInstallationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudEntitlementBundleIssueEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudInstallationCommandEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudInstallationSetupTokenEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudInstallationCommandAcknowledgementEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudInstallationHeartbeatEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ControlCloudInstallationDiagnosticReportEntityConfiguration());
    }
}
