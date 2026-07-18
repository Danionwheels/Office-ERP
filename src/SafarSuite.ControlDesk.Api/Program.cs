using SafarSuite.ControlDesk.Api.Composition;
using SafarSuite.ControlDesk.Api.Modules.Accounting;
using SafarSuite.ControlDesk.Api.Modules.Auth;
using SafarSuite.ControlDesk.Api.Modules.Billing;
using SafarSuite.ControlDesk.Api.Modules.Clients;
using SafarSuite.ControlDesk.Api.Modules.CommandCenter;
using SafarSuite.ControlDesk.Api.Modules.Contracts;
using SafarSuite.ControlDesk.Api.Modules.ControlCloud;
using SafarSuite.ControlDesk.Api.Modules.Entitlements;
using SafarSuite.ControlDesk.Api.Modules.Payments;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Health;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControlDeskServices(builder.Configuration);

var app = builder.Build();

ControlDeskHostConfigurationValidator.Validate(app.Configuration, app.Environment);

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

app.MapGet("/health", () =>
{
    var response = new HealthResponse(
        Service: "SafarSuite Control Desk API",
        Status: "Healthy",
        CheckedAtUtc: DateTimeOffset.UtcNow);

    return Results.Ok(response);
}).AllowAnonymous();

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
