using System.Reflection;
using SafarSuite.ControlDesk.Api.Modules.Auth;
using SafarSuite.ControlDesk.Application.Modules.Diagnostics.GetOfficeDiagnosticsSummary;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Diagnostics;

namespace SafarSuite.ControlDesk.Api.Modules.Diagnostics;

public static class DiagnosticsEndpoints
{
    private const string ServiceName = "SafarSuite Control Desk API";

    public static IEndpointRouteBuilder MapDiagnosticsEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api/v1/diagnostics")
            .WithTags("Diagnostics")
            .RequireAuthorization(ControlDeskPolicies.DiagnosticsRead)
            .MapGet("/summary", GetSummaryAsync);

        return endpoints;
    }

    private static async Task<IResult> GetSummaryAsync(
        HttpContext context,
        GetOfficeDiagnosticsSummaryHandler handler,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        var result = await handler.HandleAsync(cancellationToken);
        var serviceVersion = typeof(DiagnosticsEndpoints).Assembly
                                 .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                 ?.InformationalVersion
                             ?? "Unknown";
        var response = new OfficeDiagnosticsResponse(
            result.Status,
            result.CheckedAtUtc,
            new OfficeServiceDiagnosticsResponse(
                ServiceName,
                serviceVersion,
                "Running"),
            new OfficeDatabaseDiagnosticsResponse(
                result.Database.IsReady ? "Ready" : "NotReady",
                result.Database.Code,
                result.Database.PersistenceProvider,
                result.Database.ConnectivityStatus.ToString(),
                result.Database.MigrationStatus.ToString(),
                result.Database.KnownMigrationCount,
                result.Database.AppliedMigrationCount,
                result.Database.PendingMigrationCount,
                result.Database.UnknownAppliedMigrationCount),
            new OfficeOutboxDiagnosticsResponse(
                result.Outbox.Status,
                result.Outbox.TotalCount,
                result.Outbox.PendingCount,
                result.Outbox.FailedCount,
                result.Outbox.SentCount,
                result.Outbox.ReadyForPublishingCount,
                result.Outbox.TotalAttemptCount,
                new OfficeOutboxAutomationResponse(
                    result.Automation.Enabled,
                    result.Automation.Status,
                    result.Automation.StartedAtUtc,
                    result.Automation.LastCycleStartedAtUtc,
                    result.Automation.LastCycleCompletedAtUtc,
                    result.Automation.LastPublishSucceededAtUtc,
                    result.Automation.LastPublishFailedAtUtc,
                    result.Automation.LastPublishedCount,
                    result.Automation.LastFailedCount,
                    result.Automation.LastFailureCode)),
            new ControlCloudDiagnosticsResponse(
                result.ControlCloud.Status.ToString(),
                result.ControlCloud.Code,
                result.ControlCloud.HttpStatusCode,
                result.ControlCloud.LatencyMilliseconds));

        return Results.Ok(response);
    }
}
