using SafarSuite.ControlCloud.Api.Modules.Audit;
using SafarSuite.ControlCloud.Api.Modules.ClientPortal;
using SafarSuite.ControlCloud.Api.Modules.InboundControlDesk;
using SafarSuite.ControlCloud.Api.Modules.LocalServer;
using SafarSuite.ControlCloud.Api.Modules.ProviderAccess;
using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.Audit.ListControlCloudAuditEvents;
using SafarSuite.ControlCloud.Application.Modules.Audit.Ports;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.CreatePortalPaymentClaim;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.GetPortalBankDetails;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.GetPortalBillingSummary;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.GetPortalInvoice;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.GetPortalPaymentClaim;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.GetPortalPaymentClaimProof;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.ListPortalInvoices;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.ListPortalPaymentClaims;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.UploadPortalAttachment;
using SafarSuite.ControlCloud.Application.Modules.InboundControlDesk;
using SafarSuite.ControlCloud.Application.Modules.InboundControlDesk.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.AcknowledgeInstallationCommand;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.ExportOfflineRenewalFile;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.CreateInstallationSetupToken;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.CreateLocalServerBootstrapPackage;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.GetInstallationStatus;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.GetLatestInstallationDiagnostics;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.GetPendingInstallationCommands;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.IssueLocalServerFirstManagerSetupToken;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.IssueLocalServerPairingDescriptor;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.IssueSafarSuiteAppActivationToken;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.ListLocalServerBootstrapPackages;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.ListSafarSuiteAppActivationIssues;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.MarkLocalServerBootstrapPackageHandoff;
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
using System.Threading.RateLimiting;

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
var firstManagerSetupTokenOptions =
    builder.Configuration.GetSection(ControlCloudFirstManagerSetupTokenOptions.SectionName).Get<ControlCloudFirstManagerSetupTokenOptions>()
    ?? new ControlCloudFirstManagerSetupTokenOptions();
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
ApplyClientPortalSessionEnvironment(clientPortalAccessOptions);
var clientPortalInvitationDeliveryOptions =
    builder.Configuration.GetSection(ClientPortalInvitationDeliveryOptions.SectionName).Get<ClientPortalInvitationDeliveryOptions>()
    ?? new ClientPortalInvitationDeliveryOptions();
ApplySmtpEnvironment(clientPortalInvitationDeliveryOptions);
var clientPortalProviderAccessOptions = ClientPortalProviderAccessOptions.FromConfiguration(
    builder.Configuration,
    builder.Environment.ContentRootPath);
ClientPortalProductionConfigurationValidator.Validate(
    builder.Environment,
    builder.Configuration,
    clientPortalAccessOptions,
    clientPortalInvitationDeliveryOptions,
    clientPortalProviderAccessOptions);
var clientPortalAuditOptions =
    builder.Configuration.GetSection(ClientPortalAuditOptions.SectionName).Get<ClientPortalAuditOptions>()
    ?? new ClientPortalAuditOptions();

builder.Services.AddSingleton(receiverOptions);
builder.Services.AddSingleton(entitlementSigningOptions);
builder.Services.AddSingleton(commandQueueOptions);
builder.Services.AddSingleton(setupTokenOptions);
builder.Services.AddSingleton(bootstrapPackageOptions);
builder.Services.AddSingleton(firstManagerSetupTokenOptions);
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
builder.Services.AddSingleton<IControlCloudFirstManagerSetupTokenSigner, HmacControlCloudFirstManagerSetupTokenSigner>();
builder.Services.AddSingleton<IControlCloudInstallationCommandSigner, HmacControlCloudInstallationCommandSigner>();
builder.Services.AddSingleton<IControlCloudAppActivationTokenSigner, EcdsaControlCloudAppActivationTokenSigner>();
builder.Services.AddSingleton<IControlCloudAppActivationIssueRepository, FileControlCloudAppActivationIssueRepository>();
builder.Services.AddSingleton<IControlCloudFirstManagerSetupTokenIssueRepository, FileControlCloudFirstManagerSetupTokenIssueRepository>();
builder.Services.AddSingleton<IControlCloudInstallationSetupTokenService, RandomControlCloudInstallationSetupTokenService>();
builder.Services.AddSingleton<IClientPortalCredentialService, HmacClientPortalCredentialService>();
builder.Services.AddSingleton<IClientPortalTotpService, OtpNetClientPortalTotpService>();
builder.Services.AddSingleton<IClientPortalMfaSecretProtector, ClientPortalMfaSecretProtector>();
builder.Services.AddSingleton<IProviderAccessTotpSecretProtector, ProviderAccessTotpSecretProtector>();
AddClientPortalInvitationDelivery(builder.Services, clientPortalInvitationDeliveryOptions);
builder.Services.AddScoped<IClientPortalMailDeliveryQueue, ClientPortalMailDeliveryQueue>();
builder.Services.AddHostedService<ClientPortalMailDeliveryRetryProcessor>();
builder.Services.AddSingleton<FileClientPortalAuditRecorder>();
builder.Services.AddSingleton<IClientPortalAuditRecorder>(
    services => services.GetRequiredService<FileClientPortalAuditRecorder>());
builder.Services.AddSingleton<IControlCloudAuditEventReader>(
    services => services.GetRequiredService<FileClientPortalAuditRecorder>());
builder.Services.AddScoped<IClientPortalSessionService, PersistentClientPortalSessionService>();
builder.Services.AddScoped<ProviderAccessSessionService>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("client-portal-auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddSingleton<ControlCloudEnvelopeSignatureValidator>();
AddPersistence(builder.Services, builder.Configuration);
builder.Services.AddScoped<ControlDeskEnvelopeProjectionService>();
builder.Services.AddScoped<AcknowledgeInstallationCommandHandler>();
builder.Services.AddScoped<AcceptClientPortalInvitationHandler>();
builder.Services.AddScoped<CreateInstallationSetupTokenHandler>();
builder.Services.AddScoped<CreateLocalServerBootstrapPackageHandler>();
builder.Services.AddScoped<CreateClientPortalInvitationHandler>();
builder.Services.AddScoped<CreateClientPortalSessionHandler>();
builder.Services.AddScoped<BeginClientPortalMfaEnrollmentHandler>();
builder.Services.AddScoped<ConfirmClientPortalMfaEnrollmentHandler>();
builder.Services.AddScoped<RequestClientPortalPasswordResetHandler>();
builder.Services.AddScoped<ValidateClientPortalPasswordResetHandler>();
builder.Services.AddScoped<CompleteClientPortalPasswordResetHandler>();
builder.Services.AddScoped<ExportOfflineRenewalFileHandler>();
builder.Services.AddScoped<ListControlCloudAuditEventsHandler>();
builder.Services.AddScoped<ListLocalServerBootstrapPackagesHandler>();
builder.Services.AddScoped<ListSafarSuiteAppActivationIssuesHandler>();
builder.Services.AddScoped<MarkLocalServerBootstrapPackageHandoffHandler>();
builder.Services.AddScoped<ListClientPortalInvitationsHandler>();
builder.Services.AddScoped<ResendClientPortalInvitationHandler>();
builder.Services.AddScoped<RevokeClientPortalInvitationHandler>();
builder.Services.AddScoped<GetInstallationStatusHandler>();
builder.Services.AddScoped<GetLatestInstallationDiagnosticsHandler>();
builder.Services.AddScoped<GetClientPortalCommercialSummaryHandler>();
builder.Services.AddScoped<GetClientPortalCommercialDocumentsHandler>();
builder.Services.AddScoped<GetClientPortalSignedEntitlementBundleHandler>();
builder.Services.AddScoped<GetClientPortalBillingSummaryHandler>();
builder.Services.AddScoped<ListClientPortalInvoicesHandler>();
builder.Services.AddScoped<GetClientPortalInvoiceHandler>();
builder.Services.AddScoped<CreateClientPortalPaymentClaimHandler>();
builder.Services.AddScoped<ListClientPortalPaymentClaimsHandler>();
builder.Services.AddScoped<GetClientPortalPaymentClaimHandler>();
builder.Services.AddScoped<UploadClientPortalAttachmentHandler>();
builder.Services.AddScoped<GetClientPortalBankDetailsHandler>();
builder.Services.AddScoped<GetClientPortalPaymentClaimProofHandler>();
builder.Services.AddSingleton<ClientPortalAttachmentContentValidator>();
builder.Services.AddScoped<GetPendingInstallationCommandsHandler>();
builder.Services.AddScoped<IssueLocalServerFirstManagerSetupTokenHandler>();
builder.Services.AddScoped<IssueLocalServerPairingDescriptorHandler>();
builder.Services.AddScoped<IssueSafarSuiteAppActivationTokenHandler>();
builder.Services.AddScoped<QueueInstallationCommandHandler>();
builder.Services.AddScoped<ReceiveInstallationDiagnosticsHandler>();
builder.Services.AddScoped<RegisterLocalServerInstallationHandler>();
builder.Services.AddScoped<ReportInstallationHeartbeatHandler>();
builder.Services.AddScoped<RevokeSafarSuiteAppActivationIssueHandler>();
builder.Services.AddScoped<ReceiveControlDeskEnvelopeHandler>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    var isClientPortalPage = context.Request.Path.StartsWithSegments("/client-portal");
    var isClientPortalApi = context.Request.Path.StartsWithSegments("/api/v1/client-portal")
        || context.Request.Path.StartsWithSegments("/portal/api/v1");
    var isProviderPaymentClaimApi = context.Request.Path.StartsWithSegments(
        "/api/v1/provider-access/payment-claims");

    if (isClientPortalPage || isClientPortalApi || isProviderPaymentClaimApi)
    {
        context.Response.Headers.CacheControl = "no-store, max-age=0";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    }

    if (isClientPortalPage)
    {
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers.ContentSecurityPolicy =
            "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; base-uri 'none'; frame-ancestors 'none'; form-action 'self'";
    }

    await next();
});
app.UseStaticFiles();
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new
{
    service = "SafarSuite Control Cloud",
    status = "Healthy"
}));

app.MapInboundControlDeskEndpoints();
app.MapControlCloudAuditEndpoints();
app.MapProviderAccessEndpoints();
app.MapProviderPaymentClaimEndpoints();
app.MapClientPortalEndpoints();
app.MapClientPortalPaymentEndpoints();
app.MapLocalServerCommandEndpoints();

app.Run();

static void AddClientPortalInvitationDelivery(
    IServiceCollection services,
    ClientPortalInvitationDeliveryOptions options)
{
    if (options.Provider.Equals("File", StringComparison.OrdinalIgnoreCase))
    {
        services.AddSingleton<IClientPortalInvitationDeliveryRecorder, FileClientPortalInvitationDeliveryRecorder>();
        services.AddSingleton<IClientPortalMailTransport, FileClientPortalMailTransport>();
        return;
    }

    if (options.Provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
    {
        services.AddScoped<IClientPortalInvitationDeliveryRecorder, QueuedClientPortalInvitationDeliveryRecorder>();
        services.AddSingleton<IClientPortalMailTransport, SmtpClientPortalMailTransport>();
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
        services.AddScoped<IClientPortalSessionRepository, EfClientPortalSessionRepository>();
        services.AddScoped<IClientPortalPasswordResetRepository, EfClientPortalPasswordResetRepository>();
        services.AddScoped<IClientPortalMailDeliveryRepository, EfClientPortalMailDeliveryRepository>();
        services.AddScoped<IClientPortalPaymentClaimRepository, EfClientPortalPaymentClaimRepository>();
        services.AddScoped<IClientPortalAttachmentRepository, EfClientPortalAttachmentRepository>();
        services.AddScoped<IControlCloudProviderBankDetailsRepository, EfControlCloudProviderBankDetailsRepository>();
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
    services.AddSingleton<IClientPortalSessionRepository, FileClientPortalSessionRepository>();
    services.AddSingleton<IClientPortalPasswordResetRepository, FileClientPortalPasswordResetRepository>();
    services.AddSingleton<IClientPortalMailDeliveryRepository, FileClientPortalMailDeliveryRepository>();
    services.AddSingleton<IClientPortalPaymentClaimRepository, FileClientPortalPaymentClaimRepository>();
    services.AddSingleton<IClientPortalAttachmentRepository, FileClientPortalAttachmentRepository>();
    services.AddSingleton<IControlCloudProviderBankDetailsRepository, FileControlCloudProviderBankDetailsRepository>();
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

static void ApplyClientPortalSessionEnvironment(ClientPortalAccessOptions options)
{
    var configured = Environment.GetEnvironmentVariable("CLIENT_PORTAL_SESSION_IDLE_TIMEOUT_MINUTES")
        ?? Environment.GetEnvironmentVariable("SESSION_TIMEOUT_MINUTES");

    if (int.TryParse(configured, out var minutes))
    {
        options.SessionIdleTimeoutMinutes = Math.Clamp(minutes, 5, 1440);
    }

    var sessionSigningSecret = Environment.GetEnvironmentVariable(
        "CLIENT_PORTAL_SESSION_SIGNING_SECRET");

    if (!string.IsNullOrWhiteSpace(sessionSigningSecret))
    {
        options.SessionSigningSecret = sessionSigningSecret;
    }

    var mfaProtectionSecret = Environment.GetEnvironmentVariable(
        "CLIENT_PORTAL_MFA_PROTECTION_SECRET");

    if (!string.IsNullOrWhiteSpace(mfaProtectionSecret))
    {
        options.MfaProtectionSecret = mfaProtectionSecret;
    }

    options.PublicPortalUrl = Environment.GetEnvironmentVariable("CLIENT_PORTAL_PUBLIC_URL")
        ?? options.PublicPortalUrl;
}

static void ApplySmtpEnvironment(ClientPortalInvitationDeliveryOptions options)
{
    var provider = Environment.GetEnvironmentVariable(
        "CLIENT_PORTAL_INVITATION_DELIVERY_PROVIDER");

    if (!string.IsNullOrWhiteSpace(provider))
    {
        options.Provider = provider.Trim();
    }

    var host = Environment.GetEnvironmentVariable("SMTP_HOST")
        ?? Environment.GetEnvironmentVariable("CLIENT_PORTAL_SMTP_HOST");

    if (!string.IsNullOrWhiteSpace(host))
    {
        options.Provider = "Smtp";
        options.SmtpHost = host.Trim();
    }

    var configuredPort = Environment.GetEnvironmentVariable("SMTP_PORT")
        ?? Environment.GetEnvironmentVariable("CLIENT_PORTAL_SMTP_PORT");

    if (int.TryParse(configuredPort, out var port))
    {
        options.SmtpPort = Math.Clamp(port, 1, 65535);
    }

    var configuredSsl = Environment.GetEnvironmentVariable("CLIENT_PORTAL_SMTP_ENABLE_SSL");

    if (bool.TryParse(configuredSsl, out var enableSsl))
    {
        options.EnableSsl = enableSsl;
    }

    options.Username = Environment.GetEnvironmentVariable("SMTP_USER")
        ?? Environment.GetEnvironmentVariable("CLIENT_PORTAL_SMTP_USERNAME")
        ?? options.Username;
    options.Password = Environment.GetEnvironmentVariable("SMTP_PASS")
        ?? Environment.GetEnvironmentVariable("CLIENT_PORTAL_SMTP_PASSWORD")
        ?? options.Password;
    options.FromEmail = Environment.GetEnvironmentVariable("FROM_ADDRESS")
        ?? Environment.GetEnvironmentVariable("CLIENT_PORTAL_INVITATION_FROM_EMAIL")
        ?? options.FromEmail;
}
