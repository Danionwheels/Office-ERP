using SafarSuite.ControlDesk.Api.Composition;
using SafarSuite.ControlDesk.Api.Modules.Accounting;
using SafarSuite.ControlDesk.Api.Modules.Billing;
using SafarSuite.ControlDesk.Api.Modules.Clients;
using SafarSuite.ControlDesk.Api.Modules.ControlCloud;
using SafarSuite.ControlDesk.Api.Modules.Payments;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Health;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControlDeskServices();

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
app.MapAccountingEndpoints();
app.MapBillingEndpoints();
app.MapPaymentsEndpoints();
app.MapControlCloudEndpoints();

app.Run();
