namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Diagnostics;

public sealed record OfficeDiagnosticsResponse(
    string Status,
    DateTimeOffset CheckedAtUtc,
    OfficeServiceDiagnosticsResponse Service,
    OfficeDatabaseDiagnosticsResponse Database,
    OfficeOutboxDiagnosticsResponse Outbox,
    ControlCloudDiagnosticsResponse ControlCloud);

public sealed record OfficeServiceDiagnosticsResponse(
    string Name,
    string Version,
    string Status);

public sealed record OfficeDatabaseDiagnosticsResponse(
    string Status,
    string Code,
    string PersistenceProvider,
    string ConnectivityStatus,
    string MigrationStatus,
    int? KnownMigrationCount,
    int? AppliedMigrationCount,
    int? PendingMigrationCount,
    int? UnknownAppliedMigrationCount);

public sealed record OfficeOutboxDiagnosticsResponse(
    string Status,
    long? TotalCount,
    long? PendingCount,
    long? FailedCount,
    long? SentCount,
    long? ReadyForPublishingCount,
    long? TotalAttemptCount,
    OfficeOutboxAutomationResponse Automation);

public sealed record OfficeOutboxAutomationResponse(
    bool Enabled,
    string Status,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? LastCycleStartedAtUtc,
    DateTimeOffset? LastCycleCompletedAtUtc,
    DateTimeOffset? LastPublishSucceededAtUtc,
    DateTimeOffset? LastPublishFailedAtUtc,
    int LastPublishedCount,
    int LastFailedCount,
    string? LastFailureCode);

public sealed record ControlCloudDiagnosticsResponse(
    string Status,
    string Code,
    int? HttpStatusCode,
    long? LatencyMilliseconds);
