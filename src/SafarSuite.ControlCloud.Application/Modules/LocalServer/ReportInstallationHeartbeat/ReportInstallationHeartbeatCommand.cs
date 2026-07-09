using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.ReportInstallationHeartbeat;

public sealed record ReportInstallationHeartbeatCommand(
    string InstallationId,
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
    LocalServerDeploymentProfileResponse? DeploymentProfile = null,
    LocalServerPairingStatusResponse? PairingStatus = null);
