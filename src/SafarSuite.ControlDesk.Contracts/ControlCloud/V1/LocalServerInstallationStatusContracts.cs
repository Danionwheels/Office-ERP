namespace SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

public sealed record ControlCloudInstallationStatusResponse(
    Guid ClientId,
    string InstallationId,
    string InstallationStatus,
    DateTimeOffset RegisteredAtUtc,
    DateTimeOffset? LastBundleIssuedAtUtc,
    long LatestEntitlementVersion,
    LocalServerHeartbeatResponse? LatestHeartbeat,
    ControlCloudInstallationEntitlementStatusResponse? LatestEntitlement,
    ControlCloudInstallationCommandStatusResponse CommandStatus);

public sealed record ControlCloudInstallationEntitlementStatusResponse(
    Guid BundleIssueId,
    long EntitlementVersion,
    Guid EntitlementSnapshotId,
    DateTimeOffset IssuedAtUtc,
    DateOnly PaidUntil,
    DateOnly WarningStartsAt,
    DateOnly GraceUntil,
    DateOnly OfflineValidUntil,
    string KeyId,
    string PayloadSha256);

public sealed record ControlCloudInstallationCommandStatusResponse(
    int PendingCommandCount,
    long LatestCommandVersion,
    Guid? LatestCommandId,
    string? LatestCommandType,
    string? LatestCommandStatus,
    DateTimeOffset? LatestCommandQueuedAtUtc,
    DateTimeOffset? LatestCommandAcknowledgedAtUtc,
    string? LatestAcknowledgementStatus,
    string? LatestAcknowledgementDetail);
