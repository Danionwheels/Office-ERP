using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.CreateLedgerAccount;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountReconciliation;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountRepairPlan;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Accounting.SuggestLedgerAccountCode;
using SafarSuite.ControlDesk.Application.Modules.Billing.CreateChargeCode;
using SafarSuite.ControlDesk.Application.Modules.Billing.CreateClientChargeRule;
using SafarSuite.ControlDesk.Application.Modules.Billing.GenerateInvoiceDraft;
using SafarSuite.ControlDesk.Application.Modules.Billing.IssueCreditNote;
using SafarSuite.ControlDesk.Application.Modules.Billing.IssueInvoice;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.Clients.ConfigureClientAccountingProfile;
using SafarSuite.ControlDesk.Application.Modules.Clients.CreateClient;
using SafarSuite.ControlDesk.Application.Modules.Clients.GetClientStatement;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Application.Modules.Contracts;
using SafarSuite.ControlDesk.Application.Modules.Contracts.CreateClientContract;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.ApplyClientCredit;
using SafarSuite.ControlDesk.Application.Modules.Payments.Common;
using SafarSuite.ControlDesk.Application.Modules.Payments.IssueClientRefund;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.RecordInvoicePayment;
using SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;
using SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;
using SafarSuite.ControlDesk.Infrastructure.System;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.AccountingSmoke;

internal sealed class SmokeHarness : IAsyncDisposable
{
    private readonly ControlDeskDbContext? _dbContext;

    private SmokeHarness(
        IClientRepository clients,
        IClientAccountingProfileRepository clientAccountingProfiles,
        IContractRepository contracts,
        IAccountCodeRangeRepository accountCodeRanges,
        IAccountingPeriodRepository accountingPeriods,
        ILedgerAccountRepository ledgerAccounts,
        IJournalEntryRepository journalEntries,
        IChargeCodeRepository chargeCodes,
        IClientChargeRuleRepository clientChargeRules,
        IInvoiceRepository invoices,
        IPaymentRepository payments,
        ICreditNoteRepository creditNotes,
        IClientRefundRepository clientRefunds,
        IClientCreditApplicationRepository clientCreditApplications,
        ICloudOutboxMessageRepository cloudOutboxMessages,
        IUnitOfWork unitOfWork,
        ControlDeskDbContext? dbContext = null)
    {
        Clients = clients;
        ClientAccountingProfiles = clientAccountingProfiles;
        Contracts = contracts;
        AccountCodeRanges = accountCodeRanges;
        AccountingPeriods = accountingPeriods;
        LedgerAccounts = ledgerAccounts;
        JournalEntries = journalEntries;
        ChargeCodes = chargeCodes;
        ClientChargeRules = clientChargeRules;
        Invoices = invoices;
        Payments = payments;
        CreditNotes = creditNotes;
        ClientRefunds = clientRefunds;
        ClientCreditApplications = clientCreditApplications;
        CloudOutboxMessages = cloudOutboxMessages;
        UnitOfWork = unitOfWork;
        _dbContext = dbContext;

        IdGenerator = new GuidIdGenerator();
        Clock = new SystemClock();

        var postingService = new PaymentPostingService(LedgerAccounts, IdGenerator, Clock);
        var periodGuard = new AccountingPeriodPostingGuard(AccountingPeriods);
        var outboxMessageFactory = new PaymentCloudOutboxMessageFactory(IdGenerator, Clock);
        var creditBalanceService = new ClientCreditBalanceService(
            Invoices,
            CreditNotes,
            ClientRefunds,
            ClientCreditApplications);
        AccountingSetupDefaults = new AccountingSetupDefaults(
            AccountCodeRanges,
            UnitOfWork,
            IdGenerator,
            Clock);

        SuggestLedgerAccountCode = new SuggestLedgerAccountCodeHandler(
            LedgerAccounts,
            AccountCodeRanges,
            AccountingSetupDefaults);

        CreateLedgerAccount = new CreateLedgerAccountHandler(
            LedgerAccounts,
            AccountCodeRanges,
            AccountingSetupDefaults,
            UnitOfWork,
            IdGenerator,
            Clock,
            new CreateLedgerAccountValidator());

        GetLedgerAccountReconciliation = new GetLedgerAccountReconciliationHandler(
            LedgerAccounts,
            AccountCodeRanges,
            AccountingSetupDefaults);

        GetLedgerAccountRepairPlan = new GetLedgerAccountRepairPlanHandler(
            GetLedgerAccountReconciliation,
            AccountCodeRanges,
            LedgerAccounts);

        CreateClient = new CreateClientHandler(
            Clients,
            UnitOfWork,
            IdGenerator,
            Clock,
            new CreateClientValidator());

        ConfigureClientAccountingProfile = new ConfigureClientAccountingProfileHandler(
            Clients,
            ClientAccountingProfiles,
            LedgerAccounts,
            UnitOfWork,
            IdGenerator,
            Clock,
            new ConfigureClientAccountingProfileValidator());

        CreateClientContract = new CreateClientContractHandler(
            Clients,
            Contracts,
            UnitOfWork,
            IdGenerator,
            Clock,
            new CreateClientContractValidator(),
            new ProductModuleSelectionService(new EmptyProductModuleCatalog()));

        CreateChargeCode = new CreateChargeCodeHandler(
            ChargeCodes,
            LedgerAccounts,
            UnitOfWork,
            IdGenerator,
            Clock,
            new CreateChargeCodeValidator());

        CreateClientChargeRule = new CreateClientChargeRuleHandler(
            ClientChargeRules,
            ChargeCodes,
            Clients,
            UnitOfWork,
            IdGenerator,
            Clock,
            new CreateClientChargeRuleValidator());

        GenerateInvoiceDraft = new GenerateInvoiceDraftHandler(
            Invoices,
            ClientChargeRules,
            ChargeCodes,
            Clients,
            UnitOfWork,
            IdGenerator,
            Clock,
            new GenerateInvoiceDraftValidator());

        IssueInvoice = new IssueInvoiceHandler(
            Invoices,
            ChargeCodes,
            ClientAccountingProfiles,
            LedgerAccounts,
            JournalEntries,
            periodGuard,
            CloudOutboxMessages,
            UnitOfWork,
            IdGenerator,
            Clock,
            new IssueInvoiceValidator());

        RecordInvoicePayment = new RecordInvoicePaymentHandler(
            Invoices,
            Payments,
            JournalEntries,
            CloudOutboxMessages,
            periodGuard,
            postingService,
            outboxMessageFactory,
            UnitOfWork,
            IdGenerator,
            Clock,
            new RecordInvoicePaymentValidator());

        IssueCreditNote = new IssueCreditNoteHandler(
            Invoices,
            CreditNotes,
            JournalEntries,
            periodGuard,
            CloudOutboxMessages,
            UnitOfWork,
            IdGenerator,
            Clock,
            new IssueCreditNoteValidator());

        IssueClientRefund = new IssueClientRefundHandler(
            Clients,
            ClientRefunds,
            JournalEntries,
            CloudOutboxMessages,
            periodGuard,
            postingService,
            creditBalanceService,
            outboxMessageFactory,
            UnitOfWork,
            IdGenerator,
            Clock,
            new IssueClientRefundValidator());

        ApplyClientCredit = new ApplyClientCreditHandler(
            Clients,
            Invoices,
            ClientCreditApplications,
            CloudOutboxMessages,
            creditBalanceService,
            outboxMessageFactory,
            UnitOfWork,
            IdGenerator,
            Clock,
            new ApplyClientCreditValidator());

        GetClientStatement = new GetClientStatementHandler(
            Clients,
            Invoices,
            CreditNotes,
            Payments,
            ClientRefunds,
            ClientCreditApplications,
            JournalEntries);
    }

    public static async Task<SmokeHarness> CreateAsync(
        SmokeOptions options,
        CancellationToken cancellationToken = default)
    {
        if (options.Provider == "postgres")
        {
            return await CreatePostgresAsync(
                options.ConnectionString
                    ?? throw new SmokeFailureException("Postgres smoke requires a connection string."),
                cancellationToken);
        }

        return CreateInMemory();
    }

    private static SmokeHarness CreateInMemory()
    {
        return new SmokeHarness(
            new InMemoryClientRepository(),
            new InMemoryClientAccountingProfileRepository(),
            new InMemoryContractRepository(),
            new InMemoryAccountCodeRangeRepository(),
            new InMemoryAccountingPeriodRepository(),
            new InMemoryLedgerAccountRepository(),
            new InMemoryJournalEntryRepository(),
            new InMemoryChargeCodeRepository(),
            new InMemoryClientChargeRuleRepository(),
            new InMemoryInvoiceRepository(),
            new InMemoryPaymentRepository(),
            new InMemoryCreditNoteRepository(),
            new InMemoryClientRefundRepository(),
            new InMemoryClientCreditApplicationRepository(),
            new InMemoryCloudOutboxMessageRepository(),
            new NoOpUnitOfWork());
    }

    private static async Task<SmokeHarness> CreatePostgresAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<ControlDeskDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "control"))
            .Options;
        var dbContext = new ControlDeskDbContext(options);

        await dbContext.Database.MigrateAsync(cancellationToken);

        return new SmokeHarness(
            new EfClientRepository(dbContext),
            new EfClientAccountingProfileRepository(dbContext),
            new EfContractRepository(dbContext),
            new EfAccountCodeRangeRepository(dbContext),
            new EfAccountingPeriodRepository(dbContext),
            new EfLedgerAccountRepository(dbContext),
            new EfJournalEntryRepository(dbContext),
            new EfChargeCodeRepository(dbContext),
            new EfClientChargeRuleRepository(dbContext),
            new EfInvoiceRepository(dbContext),
            new EfPaymentRepository(dbContext),
            new EfCreditNoteRepository(dbContext),
            new EfClientRefundRepository(dbContext),
            new EfClientCreditApplicationRepository(dbContext),
            new EfCloudOutboxMessageRepository(dbContext),
            new EfUnitOfWork(dbContext),
            dbContext);
    }

    public IClientRepository Clients { get; }

    public IClientAccountingProfileRepository ClientAccountingProfiles { get; }

    public IContractRepository Contracts { get; }

    public IAccountCodeRangeRepository AccountCodeRanges { get; }

    public IAccountingPeriodRepository AccountingPeriods { get; }

    public ILedgerAccountRepository LedgerAccounts { get; }

    public IJournalEntryRepository JournalEntries { get; }

    public IChargeCodeRepository ChargeCodes { get; }

    public IClientChargeRuleRepository ClientChargeRules { get; }

    public IInvoiceRepository Invoices { get; }

    public IPaymentRepository Payments { get; }

    public ICreditNoteRepository CreditNotes { get; }

    public IClientRefundRepository ClientRefunds { get; }

    public IClientCreditApplicationRepository ClientCreditApplications { get; }

    public ICloudOutboxMessageRepository CloudOutboxMessages { get; }

    public IUnitOfWork UnitOfWork { get; }

    public IIdGenerator IdGenerator { get; }

    public IClock Clock { get; }

    public AccountingSetupDefaults AccountingSetupDefaults { get; }

    public CreateLedgerAccountHandler CreateLedgerAccount { get; }

    public GetLedgerAccountReconciliationHandler GetLedgerAccountReconciliation { get; }

    public GetLedgerAccountRepairPlanHandler GetLedgerAccountRepairPlan { get; }

    public SuggestLedgerAccountCodeHandler SuggestLedgerAccountCode { get; }

    public CreateClientHandler CreateClient { get; }

    public ConfigureClientAccountingProfileHandler ConfigureClientAccountingProfile { get; }

    public CreateClientContractHandler CreateClientContract { get; }

    public CreateChargeCodeHandler CreateChargeCode { get; }

    public CreateClientChargeRuleHandler CreateClientChargeRule { get; }

    public GenerateInvoiceDraftHandler GenerateInvoiceDraft { get; }

    public IssueInvoiceHandler IssueInvoice { get; }

    public RecordInvoicePaymentHandler RecordInvoicePayment { get; }

    public IssueCreditNoteHandler IssueCreditNote { get; }

    public IssueClientRefundHandler IssueClientRefund { get; }

    public ApplyClientCreditHandler ApplyClientCredit { get; }

    public GetClientStatementHandler GetClientStatement { get; }

    public async ValueTask DisposeAsync()
    {
        if (_dbContext is not null)
        {
            await _dbContext.DisposeAsync();
        }
    }

    private sealed class EmptyProductModuleCatalog : IProductModuleCatalog
    {
        public Task<IReadOnlyCollection<ProductModuleCatalogItem>> ListAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<IReadOnlyCollection<ProductModuleCatalogItem>>([]);
        }
    }
}
