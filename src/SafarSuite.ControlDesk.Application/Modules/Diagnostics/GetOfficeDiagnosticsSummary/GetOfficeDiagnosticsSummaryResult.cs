using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.Diagnostics.Ports;

namespace SafarSuite.ControlDesk.Application.Modules.Diagnostics.GetOfficeDiagnosticsSummary;

public sealed record GetOfficeDiagnosticsSummaryResult(
    string Status,
    DateTimeOffset CheckedAtUtc,
    OfficeDatabaseReadinessResult Database,
    OfficeOutboxDiagnosticsResult Outbox,
    ControlCloudReachabilityResult ControlCloud,
    CloudOutboxAutomationSnapshot Automation);

public sealed record OfficeOutboxDiagnosticsResult(
    string Status,
    long? TotalCount,
    long? PendingCount,
    long? FailedCount,
    long? SentCount,
    long? ReadyForPublishingCount,
    long? TotalAttemptCount);
