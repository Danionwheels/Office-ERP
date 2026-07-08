using SafarSuite.LocalServer.Api.Modules.LocalServer;
using SafarSuite.LocalServer.Application.Commands.GetAppActivationRevocationStatus;
using SafarSuite.LocalServer.Application.Commands.Ports;
using SafarSuite.LocalServer.Application.Commands.ProcessInstallationCommands;
using SafarSuite.LocalServer.Application.Commands.ProcessInstallationCommandsFromBootstrapConfiguration;
using SafarSuite.LocalServer.Application.Common;
using SafarSuite.LocalServer.Application.Diagnostics.CreateLocalServerDiagnosticsBundle;
using SafarSuite.LocalServer.Application.Diagnostics.Ports;
using SafarSuite.LocalServer.Application.Diagnostics.UploadDiagnosticsToControlCloud;
using SafarSuite.LocalServer.Application.Entitlements.EvaluateFeatureAccess;
using SafarSuite.LocalServer.Application.Entitlements.ImportSignedEntitlementBundle;
using SafarSuite.LocalServer.Application.Entitlements.Ports;
using SafarSuite.LocalServer.Application.Entitlements.PullEntitlementFromBootstrapConfiguration;
using SafarSuite.LocalServer.Application.Entitlements.PullEntitlementFromControlCloud;
using SafarSuite.LocalServer.Application.Heartbeats.Ports;
using SafarSuite.LocalServer.Application.Heartbeats.ReportHeartbeatFromBootstrapConfiguration;
using SafarSuite.LocalServer.Application.Heartbeats.ReportHeartbeatToControlCloud;
using SafarSuite.LocalServer.Application.ModuleGateway.EvaluateModuleAccess;
using SafarSuite.LocalServer.Application.Pairing.Ports;
using SafarSuite.LocalServer.Application.Registration.Ports;
using SafarSuite.LocalServer.Application.Registration.RegisterInstallationFromBootstrapBundle;
using SafarSuite.LocalServer.Application.Registration.RegisterInstallationWithControlCloud;
using SafarSuite.LocalServer.Domain.Entitlements;
using SafarSuite.LocalServer.Infrastructure;
using SafarSuite.LocalServer.Infrastructure.Commands;
using SafarSuite.LocalServer.Infrastructure.Diagnostics;
using SafarSuite.LocalServer.Infrastructure.Entitlements;
using SafarSuite.LocalServer.Infrastructure.Heartbeats;
using SafarSuite.LocalServer.Infrastructure.Pairing;
using SafarSuite.LocalServer.Infrastructure.Registration;

var builder = WebApplication.CreateBuilder(args);

var controlCloudOptions =
    builder.Configuration.GetSection(ControlCloudEntitlementPullOptions.SectionName).Get<ControlCloudEntitlementPullOptions>()
    ?? new ControlCloudEntitlementPullOptions();
var entitlementTrustOptions =
    builder.Configuration.GetSection(LocalServerEntitlementTrustOptions.SectionName).Get<LocalServerEntitlementTrustOptions>()
    ?? new LocalServerEntitlementTrustOptions();
var bootstrapTrustOptions =
    builder.Configuration.GetSection(LocalServerBootstrapTrustOptions.SectionName).Get<LocalServerBootstrapTrustOptions>()
    ?? new LocalServerBootstrapTrustOptions();
var commandOptions =
    builder.Configuration.GetSection(LocalServerCommandOptions.SectionName).Get<LocalServerCommandOptions>()
    ?? new LocalServerCommandOptions();
var automationOptions =
    builder.Configuration.GetSection(LocalServerRuntimeAutomationOptions.SectionName).Get<LocalServerRuntimeAutomationOptions>()
    ?? new LocalServerRuntimeAutomationOptions();
var runtimeAccessOptions = LocalServerRuntimeAccessOptions.FromConfiguration(builder.Configuration);
var pairingOptions = LocalServerPairingOptions.FromConfiguration(builder.Configuration);
var pairingStoreOptions =
    builder.Configuration.GetSection(LocalServerPairingStoreOptions.SectionName).Get<LocalServerPairingStoreOptions>()
    ?? new LocalServerPairingStoreOptions();

builder.Services.AddSingleton(controlCloudOptions);
builder.Services.AddSingleton(entitlementTrustOptions);
builder.Services.AddSingleton(bootstrapTrustOptions);
builder.Services.AddSingleton(commandOptions);
builder.Services.AddSingleton(automationOptions);
builder.Services.AddSingleton(runtimeAccessOptions);
builder.Services.AddSingleton(pairingOptions);
builder.Services.AddSingleton(pairingStoreOptions);
builder.Services.AddSingleton<ILocalServerClock, SystemLocalServerClock>();
builder.Services.AddSingleton<LocalServerEntitlementPolicy>();
builder.Services.AddSingleton<ILocalServerEntitlementCache, FileLocalServerEntitlementCache>();
builder.Services.AddSingleton<ILocalServerEntitlementTrustStateStore, FileLocalServerEntitlementTrustStateStore>();
builder.Services.AddSingleton<ILocalServerEntitlementImportAuditStore, FileLocalServerEntitlementImportAuditStore>();
builder.Services.AddSingleton<ILocalServerEntitlementBundleVerifier, HmacLocalServerEntitlementBundleVerifier>();
builder.Services.AddSingleton<ILocalServerInstallationCommandVerifier, HmacLocalServerInstallationCommandVerifier>();
builder.Services.AddSingleton<ILocalServerAppActivationRevocationStore, FileLocalServerAppActivationRevocationStore>();
builder.Services.AddSingleton<ILocalServerDevicePairingStore, FileLocalServerDevicePairingStore>();
builder.Services.AddSingleton<ILocalServerFirstManagerSetupTokenVerifier, HmacLocalServerFirstManagerSetupTokenVerifier>();
builder.Services.AddSingleton<ILocalServerBootstrapBundleVerifier, HmacLocalServerBootstrapBundleVerifier>();
builder.Services.AddSingleton<ILocalServerBootstrapConfigurationStore, FileLocalServerBootstrapConfigurationStore>();
builder.Services.AddSingleton<ILocalServerRuntimeCommandRunner, SystemLocalServerRuntimeCommandRunner>();
builder.Services.AddSingleton<ILocalServerRuntimeDiagnosticsCollector, ManifestLocalServerRuntimeDiagnosticsCollector>();
builder.Services.AddHttpClient<IControlCloudInstallationRegistrationClient, HttpControlCloudInstallationRegistrationClient>(
    client => client.BaseAddress = controlCloudOptions.BaseUrl);
builder.Services.AddHttpClient<IControlCloudHeartbeatClient, HttpControlCloudHeartbeatClient>(
    client => client.BaseAddress = controlCloudOptions.BaseUrl);
builder.Services.AddHttpClient<IControlCloudEntitlementBundleClient, HttpControlCloudEntitlementBundleClient>(
    client => client.BaseAddress = controlCloudOptions.BaseUrl);
builder.Services.AddHttpClient<IControlCloudDiagnosticsClient, HttpControlCloudDiagnosticsClient>(
    client => client.BaseAddress = controlCloudOptions.BaseUrl);
builder.Services.AddHttpClient<IControlCloudInstallationCommandClient, HttpControlCloudInstallationCommandClient>(
    client => client.BaseAddress = controlCloudOptions.BaseUrl);
builder.Services.AddScoped<ImportSignedEntitlementBundleHandler>();
builder.Services.AddScoped<PullEntitlementFromControlCloudHandler>();
builder.Services.AddScoped<PullEntitlementFromBootstrapConfigurationHandler>();
builder.Services.AddScoped<EvaluateFeatureAccessHandler>();
builder.Services.AddScoped<EvaluateModuleAccessGatewayHandler>();
builder.Services.AddScoped<GetAppActivationRevocationStatusHandler>();
builder.Services.AddScoped<CreateLocalServerDiagnosticsBundleHandler>();
builder.Services.AddScoped<UploadDiagnosticsToControlCloudHandler>();
builder.Services.AddScoped<ProcessInstallationCommandsHandler>();
builder.Services.AddScoped<ProcessInstallationCommandsFromBootstrapConfigurationHandler>();
builder.Services.AddScoped<RegisterInstallationWithControlCloudHandler>();
builder.Services.AddScoped<RegisterInstallationFromBootstrapBundleHandler>();
builder.Services.AddScoped<ReportHeartbeatToControlCloudHandler>();
builder.Services.AddScoped<ReportHeartbeatFromBootstrapConfigurationHandler>();
builder.Services.AddHostedService<LocalServerRuntimeWorker>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    service = "SafarSuite Local Server",
    status = "Healthy"
}));

app.MapLocalServerRuntimeEndpoints();

app.Run();
