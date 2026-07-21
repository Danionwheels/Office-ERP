using Microsoft.Extensions.FileProviders;
using SafarSuite.ControlDesk.Api.Composition;
using SafarSuite.ControlDesk.Api.Modules.Accounting;
using SafarSuite.ControlDesk.Api.Modules.Auth;
using SafarSuite.ControlDesk.Api.Modules.Billing;
using SafarSuite.ControlDesk.Api.Modules.Clients;
using SafarSuite.ControlDesk.Api.Modules.CommandCenter;
using SafarSuite.ControlDesk.Api.Modules.Contracts;
using SafarSuite.ControlDesk.Api.Modules.ControlCloud;
using SafarSuite.ControlDesk.Api.Modules.Diagnostics;
using SafarSuite.ControlDesk.Api.Modules.Entitlements;
using SafarSuite.ControlDesk.Api.Modules.Health;
using SafarSuite.ControlDesk.Api.Modules.Payments;
using Microsoft.Extensions.Hosting.WindowsServices;

var webApplicationOptions = WindowsServiceHelpers.IsWindowsService()
    ? new WebApplicationOptions
    {
        Args = args,
        ContentRootPath = AppContext.BaseDirectory
    }
    : new WebApplicationOptions
    {
        Args = args
    };

var builder = WebApplication.CreateBuilder(webApplicationOptions);

var installedProductionSettingsPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "SafarSuite",
    "ControlDesk",
    "Config",
    "appsettings.Production.json");
var installedProductionSettingsDirectory = Path.GetDirectoryName(installedProductionSettingsPath)!;
builder.Configuration
    .AddJsonFile(
        new PhysicalFileProvider(installedProductionSettingsDirectory),
        Path.GetFileName(installedProductionSettingsPath),
        optional: true,
        reloadOnChange: false)
    .AddEnvironmentVariables();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.AddControlDeskRetainedFileLogging();
builder.Services.AddWindowsService(options =>
    options.ServiceName = "SafarSuiteControlDeskApi");

builder.Services.AddControlDeskServices(builder.Configuration);

var app = builder.Build();

var lifecycleLogger = app.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("SafarSuite.ControlDesk.Api.Lifecycle");

try
{
    ControlDeskHostConfigurationValidator.Validate(app.Configuration, app.Environment);
}
catch (Exception exception)
{
    lifecycleLogger.LogCritical(
        "Control Desk host configuration validation failed. ExceptionType={ExceptionType} EventCode={EventCode}",
        exception.GetType().FullName,
        "OfficeHostConfigurationRejected");
    throw;
}

app.Lifetime.ApplicationStarted.Register(() =>
    lifecycleLogger.LogInformation(
        "Control Desk host started. EventCode={EventCode}",
        "OfficeHostStarted"));
app.Lifetime.ApplicationStopping.Register(() =>
    lifecycleLogger.LogInformation(
        "Control Desk host stopping. EventCode={EventCode}",
        "OfficeHostStopping"));
app.Lifetime.ApplicationStopped.Register(() =>
    lifecycleLogger.LogInformation(
        "Control Desk host stopped. EventCode={EventCode}",
        "OfficeHostStopped"));

var packagedUiIndexPath = Path.Combine(
    app.Environment.ContentRootPath,
    "wwwroot",
    "index.html");
var hasPackagedUi = File.Exists(packagedUiIndexPath);

if (hasPackagedUi)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseAuthentication();
app.UseAuthorization();

if (!hasPackagedUi)
{
    app.MapGet("/", () => Results.Redirect("/health"))
        .AllowAnonymous();
}

app.MapControlDeskHealthEndpoints();

app.MapAuthEndpoints();
app.MapClientEndpoints();
app.MapCommandCenterEndpoints();
app.MapContractEndpoints();
app.MapAccountingEndpoints();
app.MapAccountingReportEndpoints();
app.MapBillingEndpoints();
app.MapBillingReportEndpoints();
app.MapPaymentsEndpoints();
app.MapPaymentReportEndpoints();
app.MapControlCloudEndpoints();
app.MapDiagnosticsEndpoints();
app.MapEntitlementEndpoints();

if (hasPackagedUi)
{
    app.MapMethods(
            "/api/{**unmatchedApiPath}",
            ["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS", "HEAD"],
            () => Results.NotFound())
        .AllowAnonymous();

    app.MapFallbackToFile("index.html")
        .AllowAnonymous();
}

app.Run();

public partial class Program;
