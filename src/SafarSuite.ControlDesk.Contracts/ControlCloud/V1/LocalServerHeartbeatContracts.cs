namespace SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

public sealed record ReportLocalServerHeartbeatRequest(
    Guid ClientId,
    string LocalServerVersion,
    DateTimeOffset ReportedAtUtc,
    string LicenseStatus,
    long? EntitlementVersion,
    DateOnly? PaidUntil,
    DateOnly? WarningStartsAt,
    DateOnly? GraceUntil,
    DateOnly? OfflineValidUntil,
    string? Detail,
    LocalServerDeploymentProfileResponse? DeploymentProfile = null);

public sealed record LocalServerHeartbeatResponse(
    Guid HeartbeatId,
    string InstallationId,
    Guid ClientId,
    string HeartbeatStatus,
    DateTimeOffset ReceivedAtUtc,
    DateTimeOffset ReportedAtUtc,
    string LicenseStatus,
    long? EntitlementVersion,
    DateOnly? PaidUntil,
    DateOnly? WarningStartsAt,
    DateOnly? GraceUntil,
    DateOnly? OfflineValidUntil,
    string? LocalServerVersion,
    string? Detail,
    LocalServerDeploymentProfileResponse? DeploymentProfile = null);
