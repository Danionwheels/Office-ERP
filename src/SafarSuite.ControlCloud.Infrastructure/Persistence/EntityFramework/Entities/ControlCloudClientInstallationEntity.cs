namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

public sealed class ControlCloudClientInstallationEntity
{
    public string InstallationId { get; set; } = "";

    public Guid ClientId { get; set; }

    public string Status { get; set; } = "Active";

    public string? BootstrapMode { get; set; }

    public string? ClientDeploymentMode { get; set; }

    public string? SiteId { get; set; }

    public string? SiteRole { get; set; }

    public string? ParentSiteId { get; set; }

    public string? BranchCode { get; set; }

    public string? SyncTopologyId { get; set; }

    public DateTimeOffset RegisteredAtUtc { get; set; }

    public DateTimeOffset? LastBundleIssuedAtUtc { get; set; }

    public long LatestEntitlementVersion { get; set; }
}
