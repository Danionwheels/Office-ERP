using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;
using SafarSuite.ControlDesk.Domain.Modules.Entitlements;
using SafarSuite.ControlDesk.Domain.Modules.Payments;
using SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class ControlDeskDbContext : DbContext
{
    public ControlDeskDbContext(DbContextOptions<ControlDeskDbContext> options)
        : base(options)
    {
    }

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<ClientAccountingProfile> ClientAccountingProfiles => Set<ClientAccountingProfile>();

    public DbSet<ClientDeployment> ClientDeployments => Set<ClientDeployment>();

    public DbSet<AccountCodeRange> AccountCodeRanges => Set<AccountCodeRange>();

    public DbSet<AccountingControlSettings> AccountingControlSettings => Set<AccountingControlSettings>();

    public DbSet<VoucherNumberingRule> VoucherNumberingRules => Set<VoucherNumberingRule>();

    public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();

    public DbSet<LedgerAccount> LedgerAccounts => Set<LedgerAccount>();

    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();

    public DbSet<ChargeCode> ChargeCodes => Set<ChargeCode>();

    public DbSet<ClientChargeRule> ClientChargeRules => Set<ClientChargeRule>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<CreditNote> CreditNotes => Set<CreditNote>();

    public DbSet<CloudOutboxMessage> CloudOutboxMessages => Set<CloudOutboxMessage>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<ClientRefund> ClientRefunds => Set<ClientRefund>();

    public DbSet<ClientCreditApplication> ClientCreditApplications => Set<ClientCreditApplication>();

    public DbSet<EntitlementSnapshot> EntitlementSnapshots => Set<EntitlementSnapshot>();

    public DbSet<ClientContract> ClientContracts => Set<ClientContract>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("control");
        modelBuilder.ApplyConfiguration(new ClientConfiguration());
        modelBuilder.ApplyConfiguration(new ClientAccountingProfileConfiguration());
        modelBuilder.ApplyConfiguration(new ClientDeploymentConfiguration());
        modelBuilder.ApplyConfiguration(new AccountCodeRangeConfiguration());
        modelBuilder.ApplyConfiguration(new AccountingControlSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new VoucherNumberingRuleConfiguration());
        modelBuilder.ApplyConfiguration(new AccountingPeriodConfiguration());
        modelBuilder.ApplyConfiguration(new LedgerAccountConfiguration());
        modelBuilder.ApplyConfiguration(new JournalEntryConfiguration());
        modelBuilder.ApplyConfiguration(new ChargeCodeConfiguration());
        modelBuilder.ApplyConfiguration(new ClientChargeRuleConfiguration());
        modelBuilder.ApplyConfiguration(new InvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new CreditNoteConfiguration());
        modelBuilder.ApplyConfiguration(new CloudOutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
        modelBuilder.ApplyConfiguration(new ClientRefundConfiguration());
        modelBuilder.ApplyConfiguration(new ClientCreditApplicationConfiguration());
        modelBuilder.ApplyConfiguration(new EntitlementSnapshotConfiguration());
        modelBuilder.ApplyConfiguration(new ClientContractConfiguration());
        modelBuilder.ApplyConfiguration(new ProductAccessCatalogRecordConfiguration());
    }
}
