using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Modules.Accounting.CloseAccountingPeriod;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureOpeningBalanceProfile;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureAccountingControlSettings;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureDefaultAccountingControlSettings;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureVoucherNumberingRule;
using SafarSuite.ControlDesk.Application.Modules.Accounting.CreateAccountingPeriod;
using SafarSuite.ControlDesk.Application.Modules.Accounting.CreateLedgerAccount;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingPeriodCloseJournalPreview;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingPeriodCloseReadiness;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountCodeRangeValidation;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetBalanceSheet;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetJournalEntrySourceDocument;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountActivity;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountReconciliation;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountRepairPlan;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetProfitAndLossStatement;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetRevenueSummary;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetTrialBalance;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListJournalEntries;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListVoucherNumberingRules;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PostOpeningBalanceImport;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewChartOfAccountsImportText;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewJournalVoucherNumber;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewOpeningBalanceImport;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewOpeningBalanceImportText;
using SafarSuite.ControlDesk.Application.Modules.Accounting.SuggestLedgerAccountCode;
using SafarSuite.ControlDesk.Application.Modules.Billing.CreateChargeCode;
using SafarSuite.ControlDesk.Application.Modules.Billing.CreateClientChargeRule;
using SafarSuite.ControlDesk.Application.Modules.Billing.GenerateInvoiceDraft;
using SafarSuite.ControlDesk.Application.Modules.Billing.GetAccountsReceivableAging;
using SafarSuite.ControlDesk.Application.Modules.Billing.GetCreditNoteDocument;
using SafarSuite.ControlDesk.Application.Modules.Billing.GetInvoiceDocument;
using SafarSuite.ControlDesk.Application.Modules.Billing.IssueCreditNote;
using SafarSuite.ControlDesk.Application.Modules.Billing.IssueInvoice;
using SafarSuite.ControlDesk.Application.Modules.Billing.ListOutstandingInvoices;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.Clients.ConfigureClientAccountingProfile;
using SafarSuite.ControlDesk.Application.Modules.Clients.CreateClient;
using SafarSuite.ControlDesk.Application.Modules.Clients.Financials;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Application.Modules.Contracts;
using SafarSuite.ControlDesk.Application.Modules.Contracts.CreateClientContract;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Application.Modules.Contracts.ReplaceActiveClientContract;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.GetLatestEntitlementSnapshot;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.IssueEntitlementSnapshotFromPaidInvoice;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.ApplyClientCredit;
using SafarSuite.ControlDesk.Application.Modules.Payments.Common;
using SafarSuite.ControlDesk.Application.Modules.Payments.GetClientRefundDocument;
using SafarSuite.ControlDesk.Application.Modules.Payments.GetInvoicePaymentDocument;
using SafarSuite.ControlDesk.Application.Modules.Payments.IssueClientRefund;
using SafarSuite.ControlDesk.Application.Modules.Payments.ListPaymentReceiptsReport;
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
        IAccountingControlSettingsRepository accountingControlSettings,
        IVoucherNumberingRuleRepository voucherNumberingRules,
        IAccountingPeriodRepository accountingPeriods,
        IOpeningBalanceProfileRepository openingBalanceProfiles,
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
        IClientAccessRevisionRepository clientAccessRevisions,
        IEntitlementSnapshotRepository entitlementSnapshots,
        IEntitlementVersionAllocator entitlementVersions,
        IUnitOfWork unitOfWork,
        ControlDeskDbContext? dbContext = null)
    {
        Clients = clients;
        ClientAccountingProfiles = clientAccountingProfiles;
        Contracts = contracts;
        AccountCodeRanges = accountCodeRanges;
        AccountingControlSettings = accountingControlSettings;
        VoucherNumberingRules = voucherNumberingRules;
        AccountingPeriods = accountingPeriods;
        OpeningBalanceProfiles = openingBalanceProfiles;
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
        ClientAccessRevisions = clientAccessRevisions;
        EntitlementSnapshots = entitlementSnapshots;
        UnitOfWork = unitOfWork;
        _dbContext = dbContext;

        IdGenerator = new GuidIdGenerator();
        Clock = new SystemClock();

        var postingService = new PaymentPostingService(LedgerAccounts, IdGenerator, Clock);
        var periodGuard = new AccountingPeriodPostingGuard(AccountingPeriods);
        var openingBalanceProfileGuard = new OpeningBalanceProfilePostingGuard(LedgerAccounts);
        var outboxMessageFactory = new PaymentCloudOutboxMessageFactory(IdGenerator, Clock);
        IClientFinancialReader financialReader = _dbContext is null
            ? new InMemoryClientFinancialReader(
                Clients,
                Invoices,
                Payments,
                CreditNotes,
                ClientRefunds,
                ClientCreditApplications,
                JournalEntries)
            : new EfClientFinancialReader(_dbContext);
        IJournalEntryRegisterReader journalRegisterReader = _dbContext is null
            ? new InMemoryJournalEntryRegisterReader(JournalEntries)
            : new EfJournalEntryRegisterReader(_dbContext);
        IRevenueSummaryReader revenueSummaryReader = _dbContext is null
            ? new InMemoryRevenueSummaryReader(LedgerAccounts, JournalEntries)
            : new EfRevenueSummaryReader(_dbContext);
        IBillingReportReader billingReportReader = _dbContext is null
            ? new InMemoryBillingReportReader(
                (InMemoryClientRepository)Clients,
                (InMemoryInvoiceRepository)Invoices,
                JournalEntries)
            : new EfBillingReportReader(_dbContext);
        IPaymentReportReader paymentReportReader = _dbContext is null
            ? new InMemoryPaymentReportReader(
                (InMemoryPaymentRepository)Payments,
                (InMemoryClientRepository)Clients,
                (InMemoryInvoiceRepository)Invoices,
                JournalEntries)
            : new EfPaymentReportReader(_dbContext);
        var creditBalanceService = new ClientCreditBalanceService(financialReader);
        ListJournalEntries = new ListJournalEntriesHandler(journalRegisterReader);
        AccountingSetupDefaults = new AccountingSetupDefaults(
            AccountCodeRanges,
            UnitOfWork,
            IdGenerator,
            Clock);
        var voucherNumberService = new JournalVoucherNumberService(
            JournalEntries,
            VoucherNumberingRules);
        var settingsResultFactory = new AccountingControlSettingsResultFactory(LedgerAccounts);
        var openingBalanceProfileResultFactory = new OpeningBalanceProfileResultFactory(LedgerAccounts, Clock);
        var closeReadinessService = new AccountingPeriodCloseReadinessService(
            AccountingPeriods,
            JournalEntries);
        var configureOpeningBalanceProfile = new ConfigureOpeningBalanceProfileHandler(
            OpeningBalanceProfiles,
            LedgerAccounts,
            openingBalanceProfileResultFactory,
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

        ConfigureAccountingControlSettings = new ConfigureAccountingControlSettingsHandler(
            AccountingControlSettings,
            LedgerAccounts,
            settingsResultFactory,
            UnitOfWork,
            IdGenerator,
            Clock);

        ConfigureDefaultAccountingControlSettings = new ConfigureDefaultAccountingControlSettingsHandler(
            AccountingControlSettings,
            LedgerAccounts,
            AccountCodeRanges,
            AccountingSetupDefaults,
            SuggestLedgerAccountCode,
            CreateLedgerAccount,
            ConfigureAccountingControlSettings);

        ConfigureVoucherNumberingRule = new ConfigureVoucherNumberingRuleHandler(
            VoucherNumberingRules,
            UnitOfWork,
            IdGenerator,
            Clock);
        ListVoucherNumberingRules = new ListVoucherNumberingRulesHandler(VoucherNumberingRules);

        CreateAccountingPeriod = new CreateAccountingPeriodHandler(
            AccountingPeriods,
            UnitOfWork,
            IdGenerator,
            Clock,
            new CreateAccountingPeriodValidator());

        GetAccountingPeriodCloseReadiness = new GetAccountingPeriodCloseReadinessHandler(
            closeReadinessService);

        GetAccountingPeriodCloseJournalPreview = new GetAccountingPeriodCloseJournalPreviewHandler(
            AccountingPeriods,
            AccountingControlSettings,
            LedgerAccounts,
            JournalEntries);

        CloseAccountingPeriod = new CloseAccountingPeriodHandler(
            AccountingPeriods,
            closeReadinessService,
            GetAccountingPeriodCloseJournalPreview,
            JournalEntries,
            UnitOfWork,
            IdGenerator,
            Clock);

        GetLedgerAccountReconciliation = new GetLedgerAccountReconciliationHandler(
            LedgerAccounts,
            AccountCodeRanges,
            AccountingSetupDefaults);

        GetLedgerAccountRepairPlan = new GetLedgerAccountRepairPlanHandler(
            GetLedgerAccountReconciliation,
            AccountCodeRanges,
            LedgerAccounts);

        GetAccountCodeRangeValidation = new GetAccountCodeRangeValidationHandler(
            AccountCodeRanges,
            LedgerAccounts,
            AccountingSetupDefaults);

        PreviewChartOfAccountsImportText = new PreviewChartOfAccountsImportTextHandler(
            LedgerAccounts,
            AccountCodeRanges,
            AccountingSetupDefaults);

        GetJournalEntrySourceDocument = new GetJournalEntrySourceDocumentHandler(
            JournalEntries,
            OpeningBalanceProfiles,
            LedgerAccounts,
            Invoices,
            CreditNotes,
            Payments,
            ClientRefunds);

        GetLedgerAccountActivity = new GetLedgerAccountActivityHandler(
            LedgerAccounts,
            JournalEntries);

        GetTrialBalance = new GetTrialBalanceHandler(
            LedgerAccounts,
            JournalEntries,
            Clock);

        GetProfitAndLossStatement = new GetProfitAndLossStatementHandler(
            LedgerAccounts,
            JournalEntries,
            Clock);

        GetRevenueSummary = new GetRevenueSummaryHandler(
            revenueSummaryReader,
            Clock);

        GetBalanceSheet = new GetBalanceSheetHandler(
            LedgerAccounts,
            JournalEntries,
            Clock);

        PreviewJournalVoucherNumber = new PreviewJournalVoucherNumberHandler(voucherNumberService);

        PreviewOpeningBalanceImport = new PreviewOpeningBalanceImportHandler(
            LedgerAccounts,
            periodGuard,
            openingBalanceProfileGuard,
            voucherNumberService);

        PreviewOpeningBalanceImportText = new PreviewOpeningBalanceImportTextHandler(
            PreviewOpeningBalanceImport);

        PostOpeningBalanceImport = new PostOpeningBalanceImportHandler(
            PreviewOpeningBalanceImport,
            configureOpeningBalanceProfile,
            JournalEntries,
            UnitOfWork,
            IdGenerator,
            Clock);

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

        var productModuleSelection = new ProductModuleSelectionService(new EmptyProductModuleCatalog());

        CreateClientContract = new CreateClientContractHandler(
            Clients,
            Contracts,
            UnitOfWork,
            IdGenerator,
            Clock,
            new CreateClientContractValidator(),
            productModuleSelection);

        ReplaceActiveClientContract = new ReplaceActiveClientContractHandler(
            Clients,
            Contracts,
            UnitOfWork,
            IdGenerator,
            Clock,
            productModuleSelection);

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

        GetInvoiceDocument = new GetInvoiceDocumentHandler(
            Invoices,
            CreditNotes,
            JournalEntries,
            LedgerAccounts);

        GetCreditNoteDocument = new GetCreditNoteDocumentHandler(
            CreditNotes,
            Invoices,
            JournalEntries,
            LedgerAccounts);

        IssueInvoice = new IssueInvoiceHandler(
            Invoices,
            Clients,
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
            LedgerAccounts,
            CloudOutboxMessages,
            periodGuard,
            postingService,
            outboxMessageFactory,
            UnitOfWork,
            IdGenerator,
            Clock,
            new RecordInvoicePaymentValidator());

        GetInvoicePaymentDocument = new GetInvoicePaymentDocumentHandler(
            Payments,
            Invoices,
            JournalEntries,
            LedgerAccounts);

        IssueCreditNote = new IssueCreditNoteHandler(
            Invoices,
            CreditNotes,
            JournalEntries,
            LedgerAccounts,
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
            LedgerAccounts,
            CloudOutboxMessages,
            periodGuard,
            postingService,
            creditBalanceService,
            outboxMessageFactory,
            UnitOfWork,
            IdGenerator,
            Clock,
            new IssueClientRefundValidator());

        GetClientRefundDocument = new GetClientRefundDocumentHandler(
            ClientRefunds,
            JournalEntries,
            LedgerAccounts,
            creditBalanceService);

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

        GetClientFinancialSummary = new GetClientFinancialSummaryHandler(financialReader);
        ListClientInvoices = new ListClientInvoicesHandler(financialReader);
        ListClientPayments = new ListClientPaymentsHandler(financialReader);
        ListClientFinancialActivity = new ListClientFinancialActivityHandler(financialReader);
        ListClientJournalPostings = new ListClientJournalPostingsHandler(financialReader);
        GetAccountsReceivableAging = new GetAccountsReceivableAgingHandler(billingReportReader, Clock);
        ListOutstandingInvoices = new ListOutstandingInvoicesHandler(billingReportReader, Clock);
        ListPaymentReceiptsReport = new ListPaymentReceiptsReportHandler(paymentReportReader);

        IssueEntitlementSnapshot = new IssueEntitlementSnapshotFromPaidInvoiceHandler(
            Invoices,
            Contracts,
            ClientAccessRevisions,
            EntitlementSnapshots,
            entitlementVersions,
            CloudOutboxMessages,
            UnitOfWork,
            IdGenerator,
            Clock,
            new IssueEntitlementSnapshotFromPaidInvoiceValidator());

        GetLatestEntitlementSnapshot = new GetLatestEntitlementSnapshotHandler(
            EntitlementSnapshots,
            ClientAccessRevisions);
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
            new InMemoryAccountingControlSettingsRepository(),
            new InMemoryVoucherNumberingRuleRepository(),
            new InMemoryAccountingPeriodRepository(),
            new InMemoryOpeningBalanceProfileRepository(),
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
            new InMemoryClientAccessRevisionRepository(),
            new InMemoryEntitlementSnapshotRepository(),
            new InMemoryEntitlementVersionAllocator(),
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
            new EfAccountingControlSettingsRepository(dbContext),
            new EfVoucherNumberingRuleRepository(dbContext),
            new EfAccountingPeriodRepository(dbContext),
            new EfOpeningBalanceProfileRepository(dbContext),
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
            new EfClientAccessRevisionRepository(dbContext),
            new EfEntitlementSnapshotRepository(dbContext),
            new EfEntitlementVersionAllocator(dbContext),
            new EfUnitOfWork(dbContext, new EfClientWorkQueueProjector(dbContext)),
            dbContext);
    }

    public IClientRepository Clients { get; }

    public IClientAccountingProfileRepository ClientAccountingProfiles { get; }

    public IContractRepository Contracts { get; }

    public IAccountCodeRangeRepository AccountCodeRanges { get; }

    public IAccountingControlSettingsRepository AccountingControlSettings { get; }

    public IVoucherNumberingRuleRepository VoucherNumberingRules { get; }

    public IAccountingPeriodRepository AccountingPeriods { get; }

    public IOpeningBalanceProfileRepository OpeningBalanceProfiles { get; }

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

    public IClientAccessRevisionRepository ClientAccessRevisions { get; }

    public IEntitlementSnapshotRepository EntitlementSnapshots { get; }

    public IUnitOfWork UnitOfWork { get; }

    public IIdGenerator IdGenerator { get; }

    public IClock Clock { get; }

    public AccountingSetupDefaults AccountingSetupDefaults { get; }

    public CreateLedgerAccountHandler CreateLedgerAccount { get; }

    public ConfigureAccountingControlSettingsHandler ConfigureAccountingControlSettings { get; }

    public ConfigureDefaultAccountingControlSettingsHandler ConfigureDefaultAccountingControlSettings { get; }

    public ConfigureVoucherNumberingRuleHandler ConfigureVoucherNumberingRule { get; }

    public ListVoucherNumberingRulesHandler ListVoucherNumberingRules { get; }

    public CreateAccountingPeriodHandler CreateAccountingPeriod { get; }

    public GetAccountingPeriodCloseReadinessHandler GetAccountingPeriodCloseReadiness { get; }

    public GetAccountingPeriodCloseJournalPreviewHandler GetAccountingPeriodCloseJournalPreview { get; }

    public CloseAccountingPeriodHandler CloseAccountingPeriod { get; }

    public GetLedgerAccountReconciliationHandler GetLedgerAccountReconciliation { get; }

    public GetLedgerAccountRepairPlanHandler GetLedgerAccountRepairPlan { get; }

    public GetAccountCodeRangeValidationHandler GetAccountCodeRangeValidation { get; }

    public PreviewChartOfAccountsImportTextHandler PreviewChartOfAccountsImportText { get; }

    public SuggestLedgerAccountCodeHandler SuggestLedgerAccountCode { get; }

    public GetJournalEntrySourceDocumentHandler GetJournalEntrySourceDocument { get; }

    public GetLedgerAccountActivityHandler GetLedgerAccountActivity { get; }

    public GetTrialBalanceHandler GetTrialBalance { get; }

    public GetProfitAndLossStatementHandler GetProfitAndLossStatement { get; }

    public GetRevenueSummaryHandler GetRevenueSummary { get; }

    public GetBalanceSheetHandler GetBalanceSheet { get; }

    public PreviewJournalVoucherNumberHandler PreviewJournalVoucherNumber { get; }

    public PreviewOpeningBalanceImportHandler PreviewOpeningBalanceImport { get; }

    public PreviewOpeningBalanceImportTextHandler PreviewOpeningBalanceImportText { get; }

    public PostOpeningBalanceImportHandler PostOpeningBalanceImport { get; }

    public CreateClientHandler CreateClient { get; }

    public ConfigureClientAccountingProfileHandler ConfigureClientAccountingProfile { get; }

    public CreateClientContractHandler CreateClientContract { get; }

    public ReplaceActiveClientContractHandler ReplaceActiveClientContract { get; }

    public CreateChargeCodeHandler CreateChargeCode { get; }

    public CreateClientChargeRuleHandler CreateClientChargeRule { get; }

    public GenerateInvoiceDraftHandler GenerateInvoiceDraft { get; }

    public GetInvoiceDocumentHandler GetInvoiceDocument { get; }

    public GetCreditNoteDocumentHandler GetCreditNoteDocument { get; }

    public IssueInvoiceHandler IssueInvoice { get; }

    public RecordInvoicePaymentHandler RecordInvoicePayment { get; }

    public GetInvoicePaymentDocumentHandler GetInvoicePaymentDocument { get; }

    public IssueCreditNoteHandler IssueCreditNote { get; }

    public IssueClientRefundHandler IssueClientRefund { get; }

    public GetClientRefundDocumentHandler GetClientRefundDocument { get; }

    public ApplyClientCreditHandler ApplyClientCredit { get; }

    public GetClientFinancialSummaryHandler GetClientFinancialSummary { get; }

    public ListJournalEntriesHandler ListJournalEntries { get; }

    public ListClientInvoicesHandler ListClientInvoices { get; }

    public ListClientPaymentsHandler ListClientPayments { get; }

    public ListClientFinancialActivityHandler ListClientFinancialActivity { get; }

    public ListClientJournalPostingsHandler ListClientJournalPostings { get; }

    public GetAccountsReceivableAgingHandler GetAccountsReceivableAging { get; }

    public ListOutstandingInvoicesHandler ListOutstandingInvoices { get; }

    public ListPaymentReceiptsReportHandler ListPaymentReceiptsReport { get; }

    public IssueEntitlementSnapshotFromPaidInvoiceHandler IssueEntitlementSnapshot { get; }

    public GetLatestEntitlementSnapshotHandler GetLatestEntitlementSnapshot { get; }

    public async ValueTask DisposeAsync()
    {
        if (_dbContext is not null)
        {
            await _dbContext.DisposeAsync();
        }
    }

    private sealed class EmptyProductModuleCatalog : IProductModuleCatalog
    {
        private static readonly ProductCatalogRevision Revision = ProductCatalogRevision.Publish(
            ProductCatalogRevisionId.Create(Guid.Parse("9c1da88b-c763-4bb0-8dda-2d95fe63ec8f")),
            revisionNumber: 1,
            supersedesRevisionId: null,
            ProductCatalogDefinition.Create(
                [
                    ProductModuleCatalogItem.Create(
                        "BILLING",
                        "Billing",
                        ProductModuleCommercialMode.IncludedForAll,
                        isActive: true)
                ],
                new ProductAccessCatalog([], [])),
            "Accounting smoke catalog seed.",
            "Accounting smoke",
            DateTimeOffset.Parse("2026-07-11T00:00:00Z"));

        public Task<ProductCatalogRevision> GetPublishedRevisionAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(Revision);
        }

        public Task<ProductCatalogDraft?> GetDraftAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<ProductCatalogDraft?>(null);
        }

        public Task<IReadOnlyCollection<ProductCatalogRevision>> ListPublishedRevisionsAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyCollection<ProductCatalogRevision>>([Revision]);
        }

        public Task<IReadOnlyCollection<ProductModuleCatalogItem>> ListAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(Revision.Definition.Modules);
        }

        public Task<ProductAccessCatalog> GetAccessCatalogAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(Revision.Definition.AccessCatalog);
        }
    }
}
