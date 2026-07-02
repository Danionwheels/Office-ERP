namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

public sealed class ControlCloudClientInstallationEntity
{
    public string InstallationId { get; set; } = "";

    public Guid ClientId { get; set; }

    public string Status { get; set; } = "Active";

    public DateTimeOffset RegisteredAtUtc { get; set; }

    public DateTimeOffset? LastBundleIssuedAtUtc { get; set; }

    public long LatestEntitlementVersion { get; set; }
}
