namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

public sealed class ControlCloudInstallationSetupTokenEntity
{
    public Guid SetupTokenId { get; set; }

    public Guid ClientId { get; set; }

    public string InstallationId { get; set; } = "";

    public string TokenHash { get; set; } = "";

    public string Status { get; set; } = "";

    public string CreatedBy { get; set; } = "";

    public string DeploymentMode { get; set; } = "";

    public string? ClientDeploymentMode { get; set; }

    public string? SiteId { get; set; }

    public string? SiteRole { get; set; }

    public string? ParentSiteId { get; set; }

    public string? BranchCode { get; set; }

    public string? SyncTopologyId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? ConsumedAtUtc { get; set; }

    public string? ConsumedLocalServerVersion { get; set; }
}
