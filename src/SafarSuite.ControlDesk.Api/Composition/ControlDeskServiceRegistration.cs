using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Api.Modules.ControlCloud;
using SafarSuite.ControlDesk.Application.Modules.Accounting.BootstrapStandardChartOfAccounts;
using SafarSuite.ControlDesk.Application.Modules.Accounting.CloseAccountingPeriod;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureAccountingControlSettings;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureAccountCodeRange;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureDefaultAccountingControlSettings;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureOpeningBalanceProfile;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureVoucherNumberingRule;
using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.CreateAccountingPeriod;
using SafarSuite.ControlDesk.Application.Modules.Accounting.CreateLedgerAccount;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingControlSettings;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountCodeRangeValidation;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetBalanceSheet;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetJournalEntry;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetJournalEntrySourceDocument;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountReconciliation;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountRepairPlan;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountActivity;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingPeriodCloseJournalPreview;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingPeriodCloseReadiness;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetOpeningBalanceProfile;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetProfitAndLossStatement;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetTrialBalance;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListAccountCodeRanges;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListAccountingPeriods;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListJournalEntries;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListLedgerAccounts;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListVoucherNumberingRules;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PostManualJournalEntry;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PostOpeningBalanceImport;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewChartOfAccountsImportText;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewJournalVoucherNumber;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewOpeningBalanceImport;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewOpeningBalanceImportText;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ReopenAccountingPeriod;
using SafarSuite.ControlDesk.Application.Modules.Accounting.SuggestLedgerAccountCode;
using SafarSuite.ControlDesk.Application.Modules.Accounting.UpdateLedgerAccount;
using SafarSuite.ControlDesk.Application.Modules.Accounting.VoidManualJournalEntry;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Billing.CreateChargeCode;
using SafarSuite.ControlDesk.Application.Modules.Billing.CreateClientChargeRule;
using SafarSuite.ControlDesk.Application.Modules.Billing.GenerateInvoiceDraft;
using SafarSuite.ControlDesk.Application.Modules.Billing.GetCreditNoteDocument;
using SafarSuite.ControlDesk.Application.Modules.Billing.GetInvoiceDocument;
using SafarSuite.ControlDesk.Application.Modules.Billing.IssueCreditNote;
using SafarSuite.ControlDesk.Application.Modules.Billing.IssueInvoice;
using SafarSuite.ControlDesk.Application.Modules.Billing.ListChargeCodes;
using SafarSuite.ControlDesk.Application.Modules.Billing.ListClientChargeRules;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.Billing.VoidInvoice;
using SafarSuite.ControlDesk.Application.Modules.Clients.ActivateClient;
using SafarSuite.ControlDesk.Application.Modules.Clients.AddClientContact;
using SafarSuite.ControlDesk.Application.Modules.Clients.AddClientSupportNote;
using SafarSuite.ControlDesk.Application.Modules.Clients.ConfigureClientAccountingProfile;
using SafarSuite.ControlDesk.Application.Modules.Clients.ConfigureClientDeployment;
using SafarSuite.ControlDesk.Application.Modules.Clients.CreateClient;
using SafarSuite.ControlDesk.Application.Modules.Clients.GetClient;
using SafarSuite.ControlDesk.Application.Modules.Clients.GetClientAccountingProfile;
using SafarSuite.ControlDesk.Application.Modules.Clients.GetClientStatement;
using SafarSuite.ControlDesk.Application.Modules.Clients.InviteClientPortalContact;
using SafarSuite.ControlDesk.Application.Modules.Clients.ListClientContacts;
using SafarSuite.ControlDesk.Application.Modules.Clients.ListClientDeployments;
using SafarSuite.ControlDesk.Application.Modules.Clients.ListClientPortalInvitations;
using SafarSuite.ControlDesk.Application.Modules.Clients.ListClientSupportNotes;
using SafarSuite.ControlDesk.Application.Modules.Clients.ListClients;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Application.Modules.Clients.ResendClientPortalInvitation;
using SafarSuite.ControlDesk.Application.Modules.Clients.RevokeClientPortalInvitation;
using SafarSuite.ControlDesk.Application.Modules.Clients.SuspendClient;
using SafarSuite.ControlDesk.Application.Modules.Clients.UpdateClient;
using SafarSuite.ControlDesk.Application.Modules.Contracts;
using SafarSuite.ControlDesk.Application.Modules.Contracts.CreateClientContract;
using SafarSuite.ControlDesk.Application.Modules.Contracts.GetClientContract;
using SafarSuite.ControlDesk.Application.Modules.Contracts.ListClientContracts;
using SafarSuite.ControlDesk.Application.Modules.Contracts.ListProductAccessCatalog;
using SafarSuite.ControlDesk.Application.Modules.Contracts.ListProductModules;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Application.Modules.Contracts.PublishProductAccessCatalogCommand;
using SafarSuite.ControlDesk.Application.Modules.Contracts.ReplaceActiveClientContract;
using SafarSuite.ControlDesk.Application.Modules.Contracts.SaveProductAccessCatalog;
using SafarSuite.ControlDesk.Application.Modules.Contracts.SuspendClientContract;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.ChangeProviderAccessOperatorPassword;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.CreateCloudInstallationBootstrapPackage;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.CreateCloudInstallationSetupToken;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.CreateProviderAccessOperator;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.CreateProviderAccessOperatorSession;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.GetCloudInstallationDiagnostics;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.GetCloudInstallationStatus;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.IssueCloudAppActivationToken;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudAppActivationIssues;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudInstallationBootstrapPackages;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudInstallationAuditEvents;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudOutboxMessages;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListProviderAccessOperators;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.PublishPendingCloudOutboxMessages;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.QueueCloudInstallationSupportCommand;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.ResetProviderAccessOperatorPassword;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.ResetProviderAccessOperatorRecoveryCodes;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.ResetProviderAccessOperatorTotp;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.RevokeCloudAppActivationIssue;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.UpdateProviderAccessOperatorScopes;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.UpdateProviderAccessOperatorStatus;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.GetLatestEntitlementSnapshot;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.IssueEntitlementSnapshotFromPaidInvoice;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.IssueEntitlementSnapshotFromPaidInvoiceDefaults;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.ApplyClientCredit;
using SafarSuite.ControlDesk.Application.Modules.Payments.ApproveInvoicePayment;
using SafarSuite.ControlDesk.Application.Modules.Payments.Common;
using SafarSuite.ControlDesk.Application.Modules.Payments.GetClientRefundDocument;
using SafarSuite.ControlDesk.Application.Modules.Payments.GetInvoicePaymentDocument;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.RecordInvoicePayment;
using SafarSuite.ControlDesk.Application.Modules.Payments.RejectInvoicePayment;
using SafarSuite.ControlDesk.Application.Modules.Payments.ReverseInvoicePayment;
using SafarSuite.ControlDesk.Application.Modules.Payments.IssueClientRefund;
using SafarSuite.ControlDesk.Infrastructure.ControlCloud;
using SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;
using SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;
using SafarSuite.ControlDesk.Infrastructure.ProductModules;
using SafarSuite.ControlDesk.Infrastructure.System;

namespace SafarSuite.ControlDesk.Api.Composition;

public static class ControlDeskServiceRegistration
{
    public static IServiceCollection AddControlDeskServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IIdGenerator, GuidIdGenerator>();
        services.Configure<ControlCloudPublisherOptions>(
            configuration.GetSection(ControlCloudPublisherOptions.SectionName));
        services.Configure<ControlCloudStatusOptions>(
            configuration.GetSection(ControlCloudStatusOptions.SectionName));
        services.Configure<ControlCloudPortalInvitationOptions>(
            configuration.GetSection(ControlCloudPortalInvitationOptions.SectionName));
        services.Configure<ProductModuleCatalogOptions>(
            configuration.GetSection(ProductModuleCatalogOptions.SectionName));
        services.Configure<ProductKernelCommandIssuerOptions>(
            configuration.GetSection(ProductKernelCommandIssuerOptions.SectionName));
        services.AddHttpContextAccessor();
        services.AddSingleton<ControlCloudEnvelopeBuilder>();
        services.AddSingleton<ICloudOutboxPublishPolicy, ConfiguredCloudOutboxPublishPolicy>();
        services.AddSingleton<ConfiguredProductModuleCatalog>();
        services.AddScoped<IProductModuleCatalog, PersistedProductModuleCatalog>();
        services.AddScoped<
            IControlCloudProviderAccessCredentialSource,
            HttpContextControlCloudProviderAccessCredentialSource>();
        AddControlCloudPublisher(services, configuration);
        services.AddHttpClient<IControlCloudInstallationStatusClient, HttpControlCloudInstallationStatusClient>();
        services.AddHttpClient<IControlCloudInstallationProvisioningClient, HttpControlCloudInstallationProvisioningClient>();
        services.AddHttpClient<IControlCloudInstallationDiagnosticsClient, HttpControlCloudInstallationDiagnosticsClient>();
        services.AddHttpClient<IControlCloudInstallationCommandClient, HttpControlCloudInstallationCommandClient>();
        services.AddHttpClient<IControlCloudAuditClient, HttpControlCloudAuditClient>();
        services.AddHttpClient<IClientPortalInvitationClient, HttpClientPortalInvitationClient>();
        services.AddHttpClient<IControlCloudProviderAccessClient, HttpControlCloudProviderAccessClient>();
        services.AddHttpClient<IProductKernelCommandIssuerClient, HttpProductKernelCommandIssuerClient>();

        AddPersistence(services, configuration);

        services.AddScoped<CreateClientValidator>();
        services.AddScoped<CreateClientHandler>();
        services.AddScoped<ListClientsHandler>();
        services.AddScoped<GetClientHandler>();
        services.AddScoped<UpdateClientValidator>();
        services.AddScoped<UpdateClientHandler>();
        services.AddScoped<ActivateClientHandler>();
        services.AddScoped<SuspendClientHandler>();
        services.AddScoped<AddClientContactValidator>();
        services.AddScoped<AddClientContactHandler>();
        services.AddScoped<InviteClientPortalContactHandler>();
        services.AddScoped<ListClientContactsHandler>();
        services.AddScoped<ListClientPortalInvitationsHandler>();
        services.AddScoped<ResendClientPortalInvitationHandler>();
        services.AddScoped<RevokeClientPortalInvitationHandler>();
        services.AddScoped<AddClientSupportNoteValidator>();
        services.AddScoped<AddClientSupportNoteHandler>();
        services.AddScoped<ListClientSupportNotesHandler>();
        services.AddScoped<ConfigureClientAccountingProfileValidator>();
        services.AddScoped<ConfigureClientAccountingProfileHandler>();
        services.AddScoped<GetClientAccountingProfileHandler>();
        services.AddScoped<ConfigureClientDeploymentValidator>();
        services.AddScoped<ConfigureClientDeploymentHandler>();
        services.AddScoped<ListClientDeploymentsHandler>();
        services.AddScoped<GetClientStatementHandler>();
        services.AddScoped<ProductModuleSelectionService>();
        services.AddScoped<CreateClientContractValidator>();
        services.AddScoped<CreateClientContractHandler>();
        services.AddScoped<GetClientContractHandler>();
        services.AddScoped<ListClientContractsHandler>();
        services.AddScoped<ListProductAccessCatalogHandler>();
        services.AddScoped<ListProductModulesHandler>();
        services.AddScoped<PublishProductAccessCatalogCommandHandler>();
        services.AddScoped<SaveProductAccessCatalogHandler>();
        services.AddScoped<SuspendClientContractHandler>();
        services.AddScoped<ReplaceActiveClientContractHandler>();
        services.AddScoped<AccountingSetupDefaults>();
        services.AddScoped<BootstrapStandardChartOfAccountsHandler>();
        services.AddScoped<ConfigureAccountCodeRangeValidator>();
        services.AddScoped<ConfigureAccountCodeRangeHandler>();
        services.AddScoped<ListAccountCodeRangesHandler>();
        services.AddScoped<GetAccountCodeRangeValidationHandler>();
        services.AddScoped<AccountingControlSettingsResultFactory>();
        services.AddScoped<OpeningBalanceProfileResultFactory>();
        services.AddScoped<OpeningBalanceProfilePostingGuard>();
        services.AddScoped<GetAccountingControlSettingsHandler>();
        services.AddScoped<ConfigureAccountingControlSettingsHandler>();
        services.AddScoped<ConfigureDefaultAccountingControlSettingsHandler>();
        services.AddScoped<GetOpeningBalanceProfileHandler>();
        services.AddScoped<ConfigureOpeningBalanceProfileHandler>();
        services.AddScoped<ListVoucherNumberingRulesHandler>();
        services.AddScoped<ConfigureVoucherNumberingRuleHandler>();
        services.AddScoped<AccountingPeriodPostingGuard>();
        services.AddScoped<JournalVoucherNumberService>();
        services.AddScoped<AccountingPeriodCloseReadinessService>();
        services.AddScoped<GetAccountingPeriodCloseReadinessHandler>();
        services.AddScoped<GetAccountingPeriodCloseJournalPreviewHandler>();
        services.AddScoped<CreateAccountingPeriodValidator>();
        services.AddScoped<CreateAccountingPeriodHandler>();
        services.AddScoped<ListAccountingPeriodsHandler>();
        services.AddScoped<CloseAccountingPeriodHandler>();
        services.AddScoped<ReopenAccountingPeriodHandler>();
        services.AddScoped<CreateLedgerAccountValidator>();
        services.AddScoped<CreateLedgerAccountHandler>();
        services.AddScoped<UpdateLedgerAccountValidator>();
        services.AddScoped<UpdateLedgerAccountHandler>();
        services.AddScoped<ListLedgerAccountsHandler>();
        services.AddScoped<GetLedgerAccountReconciliationHandler>();
        services.AddScoped<GetLedgerAccountRepairPlanHandler>();
        services.AddScoped<PreviewChartOfAccountsImportTextHandler>();
        services.AddScoped<SuggestLedgerAccountCodeHandler>();
        services.AddScoped<ListJournalEntriesHandler>();
        services.AddScoped<GetJournalEntryHandler>();
        services.AddScoped<GetJournalEntrySourceDocumentHandler>();
        services.AddScoped<PostManualJournalEntryValidator>();
        services.AddScoped<PostManualJournalEntryHandler>();
        services.AddScoped<PreviewJournalVoucherNumberHandler>();
        services.AddScoped<PreviewOpeningBalanceImportHandler>();
        services.AddScoped<PreviewOpeningBalanceImportTextHandler>();
        services.AddScoped<PostOpeningBalanceImportHandler>();
        services.AddScoped<VoidManualJournalEntryValidator>();
        services.AddScoped<VoidManualJournalEntryHandler>();
        services.AddScoped<GetLedgerAccountActivityHandler>();
        services.AddScoped<GetTrialBalanceHandler>();
        services.AddScoped<GetProfitAndLossStatementHandler>();
        services.AddScoped<GetBalanceSheetHandler>();
        services.AddScoped<ListCloudOutboxMessagesHandler>();
        services.AddScoped<GetCloudInstallationStatusHandler>();
        services.AddScoped<GetCloudInstallationDiagnosticsHandler>();
        services.AddScoped<ListCloudInstallationBootstrapPackagesHandler>();
        services.AddScoped<ListCloudInstallationAuditEventsHandler>();
        services.AddScoped<ListCloudAppActivationIssuesHandler>();
        services.AddScoped<ListProviderAccessOperatorsHandler>();
        services.AddScoped<CreateProviderAccessOperatorHandler>();
        services.AddScoped<CreateProviderAccessOperatorSessionHandler>();
        services.AddScoped<ChangeProviderAccessOperatorPasswordHandler>();
        services.AddScoped<ResetProviderAccessOperatorPasswordHandler>();
        services.AddScoped<ResetProviderAccessOperatorRecoveryCodesHandler>();
        services.AddScoped<ResetProviderAccessOperatorTotpHandler>();
        services.AddScoped<UpdateProviderAccessOperatorScopesHandler>();
        services.AddScoped<UpdateProviderAccessOperatorStatusHandler>();
        services.AddScoped<CreateCloudInstallationSetupTokenHandler>();
        services.AddScoped<CreateCloudInstallationBootstrapPackageHandler>();
        services.AddScoped<IssueCloudAppActivationTokenHandler>();
        services.AddScoped<RevokeCloudAppActivationIssueHandler>();
        services.AddScoped<QueueCloudInstallationSupportCommandHandler>();
        services.AddScoped<PublishPendingCloudOutboxMessagesHandler>();
        services.AddScoped<IssueEntitlementSnapshotFromPaidInvoiceValidator>();
        services.AddScoped<IssueEntitlementSnapshotFromPaidInvoiceHandler>();
        services.AddScoped<IssueEntitlementSnapshotFromPaidInvoiceDefaultsHandler>();
        services.AddScoped<GetLatestEntitlementSnapshotHandler>();
        services.AddScoped<CreateChargeCodeValidator>();
        services.AddScoped<CreateChargeCodeHandler>();
        services.AddScoped<ListChargeCodesHandler>();
        services.AddScoped<CreateClientChargeRuleValidator>();
        services.AddScoped<CreateClientChargeRuleHandler>();
        services.AddScoped<ListClientChargeRulesHandler>();
        services.AddScoped<GenerateInvoiceDraftValidator>();
        services.AddScoped<GenerateInvoiceDraftHandler>();
        services.AddScoped<GetInvoiceDocumentHandler>();
        services.AddScoped<GetCreditNoteDocumentHandler>();
        services.AddScoped<IssueCreditNoteValidator>();
        services.AddScoped<IssueCreditNoteHandler>();
        services.AddScoped<IssueInvoiceValidator>();
        services.AddScoped<IssueInvoiceHandler>();
        services.AddScoped<VoidInvoiceValidator>();
        services.AddScoped<VoidInvoiceHandler>();
        services.AddScoped<PaymentPostingService>();
        services.AddScoped<ClientCreditBalanceService>();
        services.AddScoped<PaymentCloudOutboxMessageFactory>();
        services.AddScoped<RecordInvoicePaymentValidator>();
        services.AddScoped<RecordInvoicePaymentHandler>();
        services.AddScoped<ApproveInvoicePaymentValidator>();
        services.AddScoped<ApproveInvoicePaymentHandler>();
        services.AddScoped<GetInvoicePaymentDocumentHandler>();
        services.AddScoped<GetClientRefundDocumentHandler>();
        services.AddScoped<RejectInvoicePaymentValidator>();
        services.AddScoped<RejectInvoicePaymentHandler>();
        services.AddScoped<ReverseInvoicePaymentValidator>();
        services.AddScoped<ReverseInvoicePaymentHandler>();
        services.AddScoped<IssueClientRefundValidator>();
        services.AddScoped<IssueClientRefundHandler>();
        services.AddScoped<ApplyClientCreditValidator>();
        services.AddScoped<ApplyClientCreditHandler>();

        return services;
    }

    private static void AddControlCloudPublisher(IServiceCollection services, IConfiguration configuration)
    {
        var mode = configuration.GetValue<string>($"{ControlCloudPublisherOptions.SectionName}:Mode") ?? "Local";

        if (mode.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ICloudOutboxPublisher, LocalCloudOutboxPublisher>();

            return;
        }

        if (mode.Equals("Http", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<ICloudOutboxPublisher, HttpControlCloudOutboxPublisher>();

            return;
        }

        throw new InvalidOperationException(
            $"Unsupported ControlCloud:Publisher:Mode '{mode}'. Use 'Local' or 'Http'.");
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("Persistence:Provider") ?? "InMemory";

        if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration.GetConnectionString("ControlDesk");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:ControlDesk is required when Persistence:Provider is Postgres.");
            }

            services.AddDbContext<ControlDeskDbContext>(options =>
            {
                options.UseNpgsql(
                    connectionString,
                    npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "control"));
            });

            services.AddScoped<IClientRepository, EfClientRepository>();
            services.AddScoped<IContractRepository, EfContractRepository>();
            services.AddScoped<IProductAccessCatalogRepository, EfProductAccessCatalogRepository>();
            services.AddScoped<IClientAccountingProfileRepository, EfClientAccountingProfileRepository>();
            services.AddScoped<IClientDeploymentRepository, EfClientDeploymentRepository>();
            services.AddScoped<IAccountCodeRangeRepository, EfAccountCodeRangeRepository>();
            services.AddScoped<IAccountingControlSettingsRepository, EfAccountingControlSettingsRepository>();
            services.AddScoped<IOpeningBalanceProfileRepository, EfOpeningBalanceProfileRepository>();
            services.AddScoped<IVoucherNumberingRuleRepository, EfVoucherNumberingRuleRepository>();
            services.AddScoped<IAccountingPeriodRepository, EfAccountingPeriodRepository>();
            services.AddScoped<ILedgerAccountRepository, EfLedgerAccountRepository>();
            services.AddScoped<IJournalEntryRepository, EfJournalEntryRepository>();
            services.AddScoped<IChargeCodeRepository, EfChargeCodeRepository>();
            services.AddScoped<IClientChargeRuleRepository, EfClientChargeRuleRepository>();
            services.AddScoped<IInvoiceRepository, EfInvoiceRepository>();
            services.AddScoped<ICreditNoteRepository, EfCreditNoteRepository>();
            services.AddScoped<ICloudOutboxMessageRepository, EfCloudOutboxMessageRepository>();
            services.AddScoped<IPaymentRepository, EfPaymentRepository>();
            services.AddScoped<IClientRefundRepository, EfClientRefundRepository>();
            services.AddScoped<IClientCreditApplicationRepository, EfClientCreditApplicationRepository>();
            services.AddScoped<IEntitlementSnapshotRepository, EfEntitlementSnapshotRepository>();
            services.AddScoped<IUnitOfWork, EfUnitOfWork>();

            return;
        }

        if (!provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unsupported Persistence:Provider '{provider}'. Use 'InMemory' or 'Postgres'.");
        }

        services.AddSingleton<IClientRepository, InMemoryClientRepository>();
        services.AddSingleton<IContractRepository, InMemoryContractRepository>();
        services.AddSingleton<IProductAccessCatalogRepository, InMemoryProductAccessCatalogRepository>();
        services.AddSingleton<IClientAccountingProfileRepository, InMemoryClientAccountingProfileRepository>();
        services.AddSingleton<IClientDeploymentRepository, InMemoryClientDeploymentRepository>();
        services.AddSingleton<IAccountCodeRangeRepository, InMemoryAccountCodeRangeRepository>();
        services.AddSingleton<IAccountingControlSettingsRepository, InMemoryAccountingControlSettingsRepository>();
        services.AddSingleton<IOpeningBalanceProfileRepository, InMemoryOpeningBalanceProfileRepository>();
        services.AddSingleton<IVoucherNumberingRuleRepository, InMemoryVoucherNumberingRuleRepository>();
        services.AddSingleton<IAccountingPeriodRepository, InMemoryAccountingPeriodRepository>();
        services.AddSingleton<ILedgerAccountRepository, InMemoryLedgerAccountRepository>();
        services.AddSingleton<IJournalEntryRepository, InMemoryJournalEntryRepository>();
        services.AddSingleton<IChargeCodeRepository, InMemoryChargeCodeRepository>();
        services.AddSingleton<IClientChargeRuleRepository, InMemoryClientChargeRuleRepository>();
        services.AddSingleton<IInvoiceRepository, InMemoryInvoiceRepository>();
        services.AddSingleton<ICreditNoteRepository, InMemoryCreditNoteRepository>();
        services.AddSingleton<ICloudOutboxMessageRepository, InMemoryCloudOutboxMessageRepository>();
        services.AddSingleton<IPaymentRepository, InMemoryPaymentRepository>();
        services.AddSingleton<IClientRefundRepository, InMemoryClientRefundRepository>();
        services.AddSingleton<IClientCreditApplicationRepository, InMemoryClientCreditApplicationRepository>();
        services.AddSingleton<IEntitlementSnapshotRepository, InMemoryEntitlementSnapshotRepository>();
        services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
    }
}
