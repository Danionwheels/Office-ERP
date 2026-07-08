using SafarSuite.ControlCloud.Api.Modules.Audit;
using SafarSuite.ControlCloud.Api.Modules.ClientPortal;
using SafarSuite.ControlCloud.Api.Modules.InboundControlDesk;
using SafarSuite.ControlCloud.Api.Modules.LocalServer;
using SafarSuite.ControlCloud.Api.Modules.ProviderAccess;
using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.Audit.ListControlCloudAuditEvents;
using SafarSuite.ControlCloud.Application.Modules.Audit.Ports;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Application.Modules.InboundControlDesk;
using SafarSuite.ControlCloud.Application.Modules.InboundControlDesk.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.AcknowledgeInstallationCommand;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.ExportOfflineRenewalFile;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.CreateInstallationSetupToken;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.CreateLocalServerBootstrapPackage;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.GetInstallationStatus;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.GetLatestInstallationDiagnostics;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.GetPendingInstallationCommands;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.IssueSafarSuiteAppActivationToken;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.ListLocalServerBootstrapPackages;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.ListSafarSuiteAppActivationIssues;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.QueueInstallationCommand;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.ReceiveInstallationDiagnostics;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.RegisterLocalServerInstallation;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.ReportInstallationHeartbeat;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.RevokeSafarSuiteAppActivationIssue;
using SafarSuite.ControlCloud.Infrastructure;
using SafarSuite.ControlCloud.Infrastructure.ClientPortal;
using SafarSuite.ControlCloud.Infrastructure.InboundControlDesk;
using SafarSuite.ControlCloud.Infrastructure.LocalServer;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var receiverOptions =
    builder.Configuration.GetSection(ControlCloudReceiverOptions.SectionName).Get<ControlCloudReceiverOptions>()
    ?? new ControlCloudReceiverOptions();
receiverOptions.HydrateFileBackedSecrets(builder.Environment.ContentRootPath);
var entitlementSigningOptions =
    builder.Configuration.GetSection(ControlCloudEntitlementSigningOptions.SectionName).Get<ControlCloudEntitlementSigningOptions>()
    ?? new ControlCloudEntitlementSigningOptions();
entitlementSigningOptions.HydrateFileBackedSecrets(builder.Environment.ContentRootPath);
var commandQueueOptions =
    builder.Configuration.GetSection(ControlCloudCommandQueueOptions.SectionName).Get<ControlCloudCommandQueueOptions>()
    ?? new ControlCloudCommandQueueOptions();
var setupTokenOptions =
    builder.Configuration.GetSection(ControlCloudSetupTokenOptions.SectionName).Get<ControlCloudSetupTokenOptions>()
    ?? new ControlCloudSetupTokenOptions();
var bootstrapPackageOptions =
    builder.Configuration.GetSection(ControlCloudBootstrapPackageOptions.SectionName).Get<ControlCloudBootstrapPackageOptions>()
    ?? new ControlCloudBootstrapPackageOptions();
var diagnosticsOptions =
    builder.Configuration.GetSection(ControlCloudDiagnosticsOptions.SectionName).Get<ControlCloudDiagnosticsOptions>()
    ?? new ControlCloudDiagnosticsOptions();
var appActivationSigningOptions =
    builder.Configuration.GetSection(ControlCloudAppActivationSigningOptions.SectionName).Get<ControlCloudAppActivationSigningOptions>()
    ?? new ControlCloudAppActivationSigningOptions();
appActivationSigningOptions.HydrateFileBackedSecrets(builder.Environment.ContentRootPath);
var clientPortalAccessOptions =
    builder.Configuration.GetSection(ClientPortalAccessOptions.SectionName).Get<ClientPortalAccessOptions>()
    ?? new ClientPortalAccessOptions();
var clientPortalInvitationDeliveryOptions =
    builder.Configuration.GetSection(ClientPortalInvitationDeliveryOptions.SectionName).Get<ClientPortalInvitationDeliveryOptions>()
    ?? new ClientPortalInvitationDeliveryOptions();
var clientPortalAuditOptions =
    builder.Configuration.GetSection(ClientPortalAuditOptions.SectionName).Get<ClientPortalAuditOptions>()
    ?? new ClientPortalAuditOptions();
var clientPortalProviderAccessOptions = ClientPortalProviderAccessOptions.FromConfiguration(
    builder.Configuration,
    builder.Environment.ContentRootPath);

builder.Services.AddSingleton(receiverOptions);
builder.Services.AddSingleton(entitlementSigningOptions);
builder.Services.AddSingleton(commandQueueOptions);
builder.Services.AddSingleton(setupTokenOptions);
builder.Services.AddSingleton(bootstrapPackageOptions);
builder.Services.AddSingleton(diagnosticsOptions);
builder.Services.AddSingleton(appActivationSigningOptions);
builder.Services.AddSingleton(clientPortalAccessOptions);
builder.Services.AddSingleton(clientPortalInvitationDeliveryOptions);
builder.Services.AddSingleton(clientPortalAuditOptions);
builder.Services.AddSingleton(clientPortalProviderAccessOptions);
builder.Services.AddSingleton(new ControlCloudEntitlementBundleIdentity(
    entitlementSigningOptions.Issuer,
    entitlementSigningOptions.Audience));
builder.Services.AddSingleton<IControlCloudClock, SystemControlCloudClock>();
builder.Services.AddSingleton<IControlCloudSigningKeyStore, ConfiguredControlCloudSigningKeyStore>();
builder.Services.AddSingleton<IControlCloudEntitlementBundleSigner, HmacControlCloudEntitlementBundleSigner>();
builder.Services.AddSingleton<IControlCloudBootstrapPackageSigner, HmacControlCloudBootstrapPackageSigner>();
builder.Services.AddSingleton<IControlCloudInstallationCommandSigner, HmacControlCloudInstallationCommandSigner>();
builder.Services.AddSingleton<IControlCloudAppActivationTokenSigner, EcdsaControlCloudAppActivationTokenSigner>();
builder.Services.AddSingleton<IControlCloudAppActivationIssueRepository, FileControlCloudAppActivationIssueRepository>();
builder.Services.AddSingleton<IControlCloudInstallationSetupTokenService, RandomControlCloudInstallationSetupTokenService>();
builder.Services.AddSingleton<IClientPortalCredentialService, HmacClientPortalCredentialService>();
builder.Services.AddSingleton<IProviderAccessTotpSecretProtector, ProviderAccessTotpSecretProtector>();
AddClientPortalInvitationDelivery(builder.Services, clientPortalInvitationDeliveryOptions);
builder.Services.AddSingleton<FileClientPortalAuditRecorder>();
builder.Services.AddSingleton<IClientPortalAuditRecorder>(
    services => services.GetRequiredService<FileClientPortalAuditRecorder>());
builder.Services.AddSingleton<IControlCloudAuditEventReader>(
    services => services.GetRequiredService<FileClientPortalAuditRecorder>());
builder.Services.AddSingleton<IClientPortalSessionService, HmacClientPortalSessionService>();
builder.Services.AddScoped<ProviderAccessSessionService>();
builder.Services.AddSingleton<ControlCloudEnvelopeSignatureValidator>();
AddPersistence(builder.Services, builder.Configuration);
builder.Services.AddScoped<ControlDeskEnvelopeProjectionService>();
builder.Services.AddScoped<AcknowledgeInstallationCommandHandler>();
builder.Services.AddScoped<AcceptClientPortalInvitationHandler>();
builder.Services.AddScoped<CreateInstallationSetupTokenHandler>();
builder.Services.AddScoped<CreateLocalServerBootstrapPackageHandler>();
builder.Services.AddScoped<CreateClientPortalInvitationHandler>();
builder.Services.AddScoped<CreateClientPortalSessionHandler>();
builder.Services.AddScoped<ExportOfflineRenewalFileHandler>();
builder.Services.AddScoped<ListControlCloudAuditEventsHandler>();
builder.Services.AddScoped<ListLocalServerBootstrapPackagesHandler>();
builder.Services.AddScoped<ListSafarSuiteAppActivationIssuesHandler>();
builder.Services.AddScoped<ListClientPortalInvitationsHandler>();
builder.Services.AddScoped<ResendClientPortalInvitationHandler>();
builder.Services.AddScoped<RevokeClientPortalInvitationHandler>();
builder.Services.AddScoped<GetInstallationStatusHandler>();
builder.Services.AddScoped<GetLatestInstallationDiagnosticsHandler>();
builder.Services.AddScoped<GetClientPortalCommercialSummaryHandler>();
builder.Services.AddScoped<GetClientPortalSignedEntitlementBundleHandler>();
builder.Services.AddScoped<GetPendingInstallationCommandsHandler>();
builder.Services.AddScoped<IssueSafarSuiteAppActivationTokenHandler>();
builder.Services.AddScoped<QueueInstallationCommandHandler>();
builder.Services.AddScoped<ReceiveInstallationDiagnosticsHandler>();
builder.Services.AddScoped<RegisterLocalServerInstallationHandler>();
builder.Services.AddScoped<ReportInstallationHeartbeatHandler>();
builder.Services.AddScoped<RevokeSafarSuiteAppActivationIssueHandler>();
builder.Services.AddScoped<ReceiveControlDeskEnvelopeHandler>();

var app = builder.Build();

app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new
{
    service = "SafarSuite Control Cloud",
    status = "Healthy"
}));

app.MapInboundControlDeskEndpoints();
app.MapControlCloudAuditEndpoints();
app.MapProviderAccessEndpoints();
app.MapClientPortalEndpoints();
app.MapLocalServerCommandEndpoints();

app.Run();

static void AddClientPortalInvitationDelivery(
    IServiceCollection services,
    ClientPortalInvitationDeliveryOptions options)
{
    if (options.Provider.Equals("File", StringComparison.OrdinalIgnoreCase))
    {
        services.AddSingleton<IClientPortalInvitationDeliveryRecorder, FileClientPortalInvitationDeliveryRecorder>();
        return;
    }

    if (options.Provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
    {
        services.AddSingleton<IClientPortalInvitationDeliveryRecorder, SmtpClientPortalInvitationDeliveryRecorder>();
        return;
    }

    throw new InvalidOperationException(
        $"Unsupported ClientPortal:InvitationDelivery:Provider '{options.Provider}'. Use 'File' or 'Smtp'.");
}

static void AddPersistence(IServiceCollection services, IConfiguration configuration)
{
    var provider = configuration.GetValue<string>("Persistence:Provider") ?? "File";

    if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
    {
        var connectionString = configuration.GetConnectionString("ControlCloud")
            ?? configuration.GetConnectionString("ControlDesk");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:ControlCloud is required when Persistence:Provider is Postgres.");
        }

        services.AddDbContext<ControlCloudDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "cloud"));
        });

        services.AddScoped<IControlDeskEnvelopeReceiptRepository, EfControlDeskEnvelopeReceiptRepository>();
        services.AddScoped<IControlCloudClientCommercialProjectionRepository, EfControlCloudClientCommercialProjectionRepository>();
        services.AddScoped<IClientPortalIdentityRepository, EfClientPortalIdentityRepository>();
        services.AddScoped<IControlCloudClientInstallationRepository, EfControlCloudClientInstallationRepository>();
        services.AddScoped<IControlCloudEntitlementBundleIssueRepository, EfControlCloudEntitlementBundleIssueRepository>();
        services.AddScoped<IControlCloudInstallationCommandRepository, EfControlCloudInstallationCommandRepository>();
        services.AddScoped<IControlCloudInstallationSetupTokenRepository, EfControlCloudInstallationSetupTokenRepository>();
        services.AddScoped<IControlCloudInstallationCommandAcknowledgementRepository, EfControlCloudInstallationCommandAcknowledgementRepository>();
        services.AddScoped<IControlCloudInstallationHeartbeatRepository, EfControlCloudInstallationHeartbeatRepository>();
        services.AddScoped<IControlCloudInstallationDiagnosticReportRepository, EfControlCloudInstallationDiagnosticReportRepository>();
        services.AddScoped<IProviderAccessOperatorStore, EfProviderAccessOperatorStore>();
        services.AddScoped<IControlCloudUnitOfWork, EfControlCloudUnitOfWork>();

        return;
    }

    if (!provider.Equals("File", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            $"Unsupported Persistence:Provider '{provider}'. Use 'File' or 'Postgres'.");
    }

    services.AddSingleton<IControlDeskEnvelopeReceiptRepository, FileControlDeskEnvelopeReceiptRepository>();
    services.AddSingleton<IControlCloudClientCommercialProjectionRepository, FileControlCloudClientCommercialProjectionRepository>();
    services.AddSingleton<IClientPortalIdentityRepository, FileClientPortalIdentityRepository>();
    services.AddSingleton<IControlCloudClientInstallationRepository, FileControlCloudClientInstallationRepository>();
    services.AddSingleton<IControlCloudEntitlementBundleIssueRepository, FileControlCloudEntitlementBundleIssueRepository>();
    services.AddSingleton<IControlCloudInstallationCommandRepository, FileControlCloudInstallationCommandRepository>();
    services.AddSingleton<IControlCloudInstallationSetupTokenRepository, FileControlCloudInstallationSetupTokenRepository>();
    services.AddSingleton<IControlCloudInstallationCommandAcknowledgementRepository, FileControlCloudInstallationCommandAcknowledgementRepository>();
    services.AddSingleton<IControlCloudInstallationHeartbeatRepository, FileControlCloudInstallationHeartbeatRepository>();
    services.AddSingleton<IControlCloudInstallationDiagnosticReportRepository, FileControlCloudInstallationDiagnosticReportRepository>();
    services.AddSingleton<IProviderAccessOperatorStore, FileProviderAccessOperatorStore>();
    services.AddSingleton<IControlCloudUnitOfWork, FileControlCloudUnitOfWork>();
}
