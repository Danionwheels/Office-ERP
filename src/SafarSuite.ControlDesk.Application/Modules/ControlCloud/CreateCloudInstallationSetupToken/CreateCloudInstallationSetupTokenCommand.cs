namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.CreateCloudInstallationSetupToken;

public sealed record CreateCloudInstallationSetupTokenCommand(
    Guid ClientId,
    string InstallationId,
    int ExpiresInHours,
    string CreatedBy,
    string DeploymentMode,
    string? ClientDeploymentMode = null,
    string? SiteId = null,
    string? SiteRole = null,
    string? ParentSiteId = null,
    string? BranchCode = null,
    string? SyncTopologyId = null);
