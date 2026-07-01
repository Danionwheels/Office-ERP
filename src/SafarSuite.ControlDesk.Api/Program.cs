using SafarSuite.ControlDesk.Api.Composition;
using SafarSuite.ControlDesk.Api.Modules.Accounting;
using SafarSuite.ControlDesk.Api.Modules.Billing;
using SafarSuite.ControlDesk.Api.Modules.Clients;
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

app.MapGet("/", () => Results.Redirect("/health"));

app.MapGet("/health", () =>
{
    var response = new HealthResponse(
        Service: "SafarSuite Control Desk API",
        Status: "Healthy",
        CheckedAtUtc: DateTimeOffset.UtcNow);

    return Results.Ok(response);
});

app.MapClientEndpoints();
app.MapContractEndpoints();
app.MapAccountingEndpoints();
app.MapBillingEndpoints();
app.MapPaymentsEndpoints();
app.MapControlCloudEndpoints();
app.MapEntitlementEndpoints();

app.Run();
