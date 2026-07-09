namespace SafarSuite.ControlCloud.Domain.Modules.LocalServer;

public sealed record ControlCloudInstallationHeartbeat(
    Guid HeartbeatId,
    Guid ClientId,
    string InstallationId,
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
    ControlCloudInstallationPairingStatus? PairingStatus = null);

public sealed record ControlCloudInstallationPairingStatus(
    string PairingMode,
    int TotalDeviceCount,
    int PendingDeviceCount,
    int ApprovedDeviceCount,
    int SuspendedDeviceCount,
    int RevokedDeviceCount,
    bool FirstManagerDeviceApproved,
    DateTimeOffset? FirstManagerDeviceApprovedAtUtc,
    DateTimeOffset? LastDeviceUpdatedAtUtc);

public static class ControlCloudInstallationHeartbeatStatuses
{
    public const string Received = "Received";
}
