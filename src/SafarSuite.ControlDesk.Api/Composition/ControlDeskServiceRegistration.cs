using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Modules.Accounting.CreateLedgerAccount;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountActivity;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListJournalEntries;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Billing.CreateChargeCode;
using SafarSuite.ControlDesk.Application.Modules.Billing.CreateClientChargeRule;
using SafarSuite.ControlDesk.Application.Modules.Billing.GenerateInvoiceDraft;
using SafarSuite.ControlDesk.Application.Modules.Billing.IssueInvoice;
using SafarSuite.ControlDesk.Application.Modules.Billing.ListChargeCodes;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.Clients.ActivateClient;
using SafarSuite.ControlDesk.Application.Modules.Clients.AddClientContact;
using SafarSuite.ControlDesk.Application.Modules.Clients.AddClientSupportNote;
using SafarSuite.ControlDesk.Application.Modules.Clients.ConfigureClientAccountingProfile;
using SafarSuite.ControlDesk.Application.Modules.Clients.CreateClient;
using SafarSuite.ControlDesk.Application.Modules.Clients.GetClient;
using SafarSuite.ControlDesk.Application.Modules.Clients.GetClientAccountingProfile;
using SafarSuite.ControlDesk.Application.Modules.Clients.GetClientStatement;
using SafarSuite.ControlDesk.Application.Modules.Clients.ListClientContacts;
using SafarSuite.ControlDesk.Application.Modules.Clients.ListClientSupportNotes;
using SafarSuite.ControlDesk.Application.Modules.Clients.ListClients;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Application.Modules.Clients.SuspendClient;
using SafarSuite.ControlDesk.Application.Modules.Clients.UpdateClient;
using SafarSuite.ControlDesk.Application.Modules.Contracts.CreateClientContract;
using SafarSuite.ControlDesk.Application.Modules.Contracts.GetClientContract;
using SafarSuite.ControlDesk.Application.Modules.Contracts.ListClientContracts;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Application.Modules.Contracts.ReplaceActiveClientContract;
using SafarSuite.ControlDesk.Application.Modules.Contracts.SuspendClientContract;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudOutboxMessages;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.PublishPendingCloudOutboxMessages;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.GetLatestEntitlementSnapshot;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.IssueEntitlementSnapshotFromPaidInvoice;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.IssueEntitlementSnapshotFromPaidInvoiceDefaults;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.RecordInvoicePayment;
using SafarSuite.ControlDesk.Infrastructure.ControlCloud;
using SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;
using SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;
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
        services.AddSingleton<ControlCloudEnvelopeBuilder>();
        services.AddSingleton<ICloudOutboxPublishPolicy, ConfiguredCloudOutboxPublishPolicy>();
        AddControlCloudPublisher(services, configuration);

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
        services.AddScoped<ListClientContactsHandler>();
        services.AddScoped<AddClientSupportNoteValidator>();
        services.AddScoped<AddClientSupportNoteHandler>();
        services.AddScoped<ListClientSupportNotesHandler>();
        services.AddScoped<ConfigureClientAccountingProfileValidator>();
        services.AddScoped<ConfigureClientAccountingProfileHandler>();
        services.AddScoped<GetClientAccountingProfileHandler>();
        services.AddScoped<GetClientStatementHandler>();
        services.AddScoped<CreateClientContractValidator>();
        services.AddScoped<CreateClientContractHandler>();
        services.AddScoped<GetClientContractHandler>();
        services.AddScoped<ListClientContractsHandler>();
        services.AddScoped<SuspendClientContractHandler>();
        services.AddScoped<ReplaceActiveClientContractHandler>();
        services.AddScoped<CreateLedgerAccountValidator>();
        services.AddScoped<CreateLedgerAccountHandler>();
        services.AddScoped<ListJournalEntriesHandler>();
        services.AddScoped<GetLedgerAccountActivityHandler>();
        services.AddScoped<ListCloudOutboxMessagesHandler>();
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
        services.AddScoped<GenerateInvoiceDraftValidator>();
        services.AddScoped<GenerateInvoiceDraftHandler>();
        services.AddScoped<IssueInvoiceValidator>();
        services.AddScoped<IssueInvoiceHandler>();
        services.AddScoped<RecordInvoicePaymentValidator>();
        services.AddScoped<RecordInvoicePaymentHandler>();

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
            services.AddScoped<IClientAccountingProfileRepository, EfClientAccountingProfileRepository>();
            services.AddScoped<ILedgerAccountRepository, EfLedgerAccountRepository>();
            services.AddScoped<IJournalEntryRepository, EfJournalEntryRepository>();
            services.AddScoped<IChargeCodeRepository, EfChargeCodeRepository>();
            services.AddScoped<IClientChargeRuleRepository, EfClientChargeRuleRepository>();
            services.AddScoped<IInvoiceRepository, EfInvoiceRepository>();
            services.AddScoped<ICloudOutboxMessageRepository, EfCloudOutboxMessageRepository>();
            services.AddScoped<IPaymentRepository, EfPaymentRepository>();
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
        services.AddSingleton<IClientAccountingProfileRepository, InMemoryClientAccountingProfileRepository>();
        services.AddSingleton<ILedgerAccountRepository, InMemoryLedgerAccountRepository>();
        services.AddSingleton<IJournalEntryRepository, InMemoryJournalEntryRepository>();
        services.AddSingleton<IChargeCodeRepository, InMemoryChargeCodeRepository>();
        services.AddSingleton<IClientChargeRuleRepository, InMemoryClientChargeRuleRepository>();
        services.AddSingleton<IInvoiceRepository, InMemoryInvoiceRepository>();
        services.AddSingleton<ICloudOutboxMessageRepository, InMemoryCloudOutboxMessageRepository>();
        services.AddSingleton<IPaymentRepository, InMemoryPaymentRepository>();
        services.AddSingleton<IEntitlementSnapshotRepository, InMemoryEntitlementSnapshotRepository>();
        services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
    }
}
