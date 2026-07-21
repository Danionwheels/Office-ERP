namespace SafarSuite.ControlDesk.Application.Modules.Diagnostics.Ports;

public sealed record OfficeDatabaseReadinessResult(
    bool IsReady,
    string Code,
    string PersistenceProvider,
    OfficeDatabaseConnectivityStatus ConnectivityStatus,
    OfficeDatabaseMigrationStatus MigrationStatus,
    int? KnownMigrationCount,
    int? AppliedMigrationCount,
    int? PendingMigrationCount,
    int? UnknownAppliedMigrationCount);

public enum OfficeDatabaseConnectivityStatus
{
    Ready,
    Unavailable,
    NotApplicable,
    Indeterminate
}

public enum OfficeDatabaseMigrationStatus
{
    Current,
    Pending,
    UnknownApplied,
    OrderMismatch,
    NotApplicable,
    TimedOut,
    InspectionFailed,
    Indeterminate
}
