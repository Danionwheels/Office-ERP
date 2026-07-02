namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

public sealed class ControlCloudInstallationHeartbeatEntity
{
    public Guid HeartbeatId { get; set; }

    public Guid ClientId { get; set; }

    public string InstallationId { get; set; } = "";

    public string HeartbeatStatus { get; set; } = "Received";

    public DateTimeOffset ReceivedAtUtc { get; set; }

    public DateTimeOffset ReportedAtUtc { get; set; }

    public string LicenseStatus { get; set; } = "Unknown";

    public long? EntitlementVersion { get; set; }

    public DateOnly? PaidUntil { get; set; }

    public DateOnly? WarningStartsAt { get; set; }

    public DateOnly? GraceUntil { get; set; }

    public DateOnly? OfflineValidUntil { get; set; }

    public string? LocalServerVersion { get; set; }

    public string? Detail { get; set; }
}
