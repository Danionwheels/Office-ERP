using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.Diagnostics.Ports;

namespace SafarSuite.ControlDesk.Application.Modules.Diagnostics.GetOfficeDiagnosticsSummary;

public sealed class GetOfficeDiagnosticsSummaryHandler
{
    private readonly IOfficeDatabaseReadinessProbe _databaseReadiness;
    private readonly IControlCloudReachabilityProbe _cloudReachability;
    private readonly ICloudOutboxMessageRepository _outboxMessages;
    private readonly ICloudOutboxPublishPolicy _publishPolicy;
    private readonly ICloudOutboxAutomationState _automationState;
    private readonly IClock _clock;

    public GetOfficeDiagnosticsSummaryHandler(
        IOfficeDatabaseReadinessProbe databaseReadiness,
        IControlCloudReachabilityProbe cloudReachability,
        ICloudOutboxMessageRepository outboxMessages,
        ICloudOutboxPublishPolicy publishPolicy,
        ICloudOutboxAutomationState automationState,
        IClock clock)
    {
        _databaseReadiness = databaseReadiness;
        _cloudReachability = cloudReachability;
        _outboxMessages = outboxMessages;
        _publishPolicy = publishPolicy;
        _automationState = automationState;
        _clock = clock;
    }

    public async Task<GetOfficeDiagnosticsSummaryResult> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var databaseTask = _databaseReadiness.CheckAsync(cancellationToken);
        var cloudTask = _cloudReachability.CheckAsync(cancellationToken);

        await Task.WhenAll(databaseTask, cloudTask);

        var database = await databaseTask;
        var cloud = await cloudTask;
        var automation = _automationState.GetSnapshot();
        var outbox = await ReadOutboxSummaryAsync(database, automation, cancellationToken);
        var status = ResolveOverallStatus(database, cloud, outbox, automation);

        return new GetOfficeDiagnosticsSummaryResult(
            status,
            _clock.UtcNow,
            database,
            outbox,
            cloud,
            automation);
    }

    private async Task<OfficeOutboxDiagnosticsResult> ReadOutboxSummaryAsync(
        OfficeDatabaseReadinessResult database,
        CloudOutboxAutomationSnapshot automation,
        CancellationToken cancellationToken)
    {
        if (!database.IsReady)
        {
            return UnavailableOutbox();
        }

        try
        {
            var summary = await _outboxMessages.SummarizeAsync(
                status: null,
                messageType: null,
                clientId: null,
                _clock.UtcNow,
                _publishPolicy.MaximumAttemptCount,
                cancellationToken);

            var automationCannotPublish = !automation.Enabled
                                          || automation.Status is "Disabled" or "Stopped" or "Faulted";
            var requiresAttention = summary.FailedCount > 0
                                    || (summary.ReadyForPublishingCount > 0
                                        && automationCannotPublish);

            return new OfficeOutboxDiagnosticsResult(
                requiresAttention ? "AttentionRequired" : "Healthy",
                summary.TotalCount,
                summary.PendingCount,
                summary.FailedCount,
                summary.SentCount,
                summary.ReadyForPublishingCount,
                summary.TotalAttemptCount);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return UnavailableOutbox();
        }
    }

    private static string ResolveOverallStatus(
        OfficeDatabaseReadinessResult database,
        ControlCloudReachabilityResult cloud,
        OfficeOutboxDiagnosticsResult outbox,
        CloudOutboxAutomationSnapshot automation)
    {
        if (!database.IsReady)
        {
            return "Unavailable";
        }

        return !cloud.IsReachable
               || outbox.Status != "Healthy"
               || string.Equals(automation.Status, "Faulted", StringComparison.Ordinal)
            ? "Degraded"
            : "Healthy";
    }

    private static OfficeOutboxDiagnosticsResult UnavailableOutbox() =>
        new("Unavailable", null, null, null, null, null, null);
}
