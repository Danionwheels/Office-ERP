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
    string? Detail);

public static class ControlCloudInstallationHeartbeatStatuses
{
    public const string Received = "Received";
}
