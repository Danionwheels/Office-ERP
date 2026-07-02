using SafarSuite.ControlCloud.Api.Modules.ClientPortal;
using SafarSuite.ControlCloud.Api.Modules.InboundControlDesk;
using SafarSuite.ControlCloud.Api.Modules.LocalServer;
using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Application.Modules.InboundControlDesk;
using SafarSuite.ControlCloud.Application.Modules.InboundControlDesk.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.AcknowledgeInstallationCommand;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.GetInstallationStatus;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.GetPendingInstallationCommands;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.QueueInstallationCommand;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.ReportInstallationHeartbeat;
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
var entitlementSigningOptions =
    builder.Configuration.GetSection(ControlCloudEntitlementSigningOptions.SectionName).Get<ControlCloudEntitlementSigningOptions>()
    ?? new ControlCloudEntitlementSigningOptions();
var commandQueueOptions =
    builder.Configuration.GetSection(ControlCloudCommandQueueOptions.SectionName).Get<ControlCloudCommandQueueOptions>()
    ?? new ControlCloudCommandQueueOptions();
var clientPortalAccessOptions =
    builder.Configuration.GetSection(ClientPortalAccessOptions.SectionName).Get<ClientPortalAccessOptions>()
    ?? new ClientPortalAccessOptions();
var clientPortalProviderAccessOptions =
    builder.Configuration.GetSection(ClientPortalProviderAccessOptions.SectionName).Get<ClientPortalProviderAccessOptions>()
    ?? new ClientPortalProviderAccessOptions();

builder.Services.AddSingleton(receiverOptions);
builder.Services.AddSingleton(entitlementSigningOptions);
builder.Services.AddSingleton(commandQueueOptions);
builder.Services.AddSingleton(clientPortalAccessOptions);
builder.Services.AddSingleton(clientPortalProviderAccessOptions);
builder.Services.AddSingleton(new ControlCloudEntitlementBundleIdentity(
    entitlementSigningOptions.Issuer,
    entitlementSigningOptions.Audience));
builder.Services.AddSingleton<IControlCloudClock, SystemControlCloudClock>();
builder.Services.AddSingleton<IControlCloudSigningKeyStore, ConfiguredControlCloudSigningKeyStore>();
builder.Services.AddSingleton<IControlCloudEntitlementBundleSigner, HmacControlCloudEntitlementBundleSigner>();
builder.Services.AddSingleton<IControlCloudInstallationCommandSigner, HmacControlCloudInstallationCommandSigner>();
builder.Services.AddSingleton<IClientPortalCredentialService, HmacClientPortalCredentialService>();
builder.Services.AddSingleton<IClientPortalSessionService, HmacClientPortalSessionService>();
builder.Services.AddSingleton<ControlCloudEnvelopeSignatureValidator>();
AddPersistence(builder.Services, builder.Configuration);
builder.Services.AddScoped<ControlDeskEnvelopeProjectionService>();
builder.Services.AddScoped<AcknowledgeInstallationCommandHandler>();
builder.Services.AddScoped<AcceptClientPortalInvitationHandler>();
builder.Services.AddScoped<CreateClientPortalInvitationHandler>();
builder.Services.AddScoped<CreateClientPortalSessionHandler>();
builder.Services.AddScoped<GetInstallationStatusHandler>();
builder.Services.AddScoped<GetClientPortalCommercialSummaryHandler>();
builder.Services.AddScoped<GetClientPortalSignedEntitlementBundleHandler>();
builder.Services.AddScoped<GetPendingInstallationCommandsHandler>();
builder.Services.AddScoped<QueueInstallationCommandHandler>();
builder.Services.AddScoped<ReportInstallationHeartbeatHandler>();
builder.Services.AddScoped<ReceiveControlDeskEnvelopeHandler>();

var app = builder.Build();

app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new
{
    service = "SafarSuite Control Cloud",
    status = "Healthy"
}));

app.MapInboundControlDeskEndpoints();
app.MapClientPortalEndpoints();
app.MapLocalServerCommandEndpoints();

app.Run();

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
        services.AddScoped<IControlCloudInstallationCommandAcknowledgementRepository, EfControlCloudInstallationCommandAcknowledgementRepository>();
        services.AddScoped<IControlCloudInstallationHeartbeatRepository, EfControlCloudInstallationHeartbeatRepository>();
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
    services.AddSingleton<IControlCloudInstallationCommandAcknowledgementRepository, FileControlCloudInstallationCommandAcknowledgementRepository>();
    services.AddSingleton<IControlCloudInstallationHeartbeatRepository, FileControlCloudInstallationHeartbeatRepository>();
    services.AddSingleton<IControlCloudUnitOfWork, FileControlCloudUnitOfWork>();
}
