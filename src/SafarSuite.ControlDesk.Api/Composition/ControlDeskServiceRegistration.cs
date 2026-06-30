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
using SafarSuite.ControlDesk.Application.Modules.Clients.ListClientContacts;
using SafarSuite.ControlDesk.Application.Modules.Clients.ListClientSupportNotes;
using SafarSuite.ControlDesk.Application.Modules.Clients.ListClients;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Application.Modules.Clients.SuspendClient;
using SafarSuite.ControlDesk.Application.Modules.Clients.UpdateClient;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudOutboxMessages;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.RecordInvoicePayment;
using SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;
using SafarSuite.ControlDesk.Infrastructure.System;

namespace SafarSuite.ControlDesk.Api.Composition;

public static class ControlDeskServiceRegistration
{
    public static IServiceCollection AddControlDeskServices(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IIdGenerator, GuidIdGenerator>();

        services.AddSingleton<IClientRepository, InMemoryClientRepository>();
        services.AddSingleton<IClientAccountingProfileRepository, InMemoryClientAccountingProfileRepository>();
        services.AddSingleton<ILedgerAccountRepository, InMemoryLedgerAccountRepository>();
        services.AddSingleton<IChargeCodeRepository, InMemoryChargeCodeRepository>();
        services.AddSingleton<IClientChargeRuleRepository, InMemoryClientChargeRuleRepository>();
        services.AddSingleton<IInvoiceRepository, InMemoryInvoiceRepository>();
        services.AddSingleton<IJournalEntryRepository, InMemoryJournalEntryRepository>();
        services.AddSingleton<ICloudOutboxMessageRepository, InMemoryCloudOutboxMessageRepository>();
        services.AddSingleton<IPaymentRepository, InMemoryPaymentRepository>();
        services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();

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
        services.AddScoped<CreateLedgerAccountValidator>();
        services.AddScoped<CreateLedgerAccountHandler>();
        services.AddScoped<ListJournalEntriesHandler>();
        services.AddScoped<GetLedgerAccountActivityHandler>();
        services.AddScoped<ListCloudOutboxMessagesHandler>();
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
}
