namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.CreateInstallationSetupToken;

public sealed record CreateInstallationSetupTokenCommand(
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
