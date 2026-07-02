namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.CreateLocalServerBootstrapPackage;

public sealed record CreateLocalServerBootstrapPackageCommand(
    Guid ClientId,
    string InstallationId,
    int ExpiresInHours,
    string CreatedBy,
    string DeploymentMode,
    string LocalServerVersion,
    string CloudBaseUrl,
    string InstallScriptUrl,
    string? SafarSuiteAppVersion = null,
    string? ClientDeploymentMode = null,
    string? SiteId = null,
    string? SiteRole = null,
    string? ParentSiteId = null,
    string? BranchCode = null,
    string? SyncTopologyId = null);
