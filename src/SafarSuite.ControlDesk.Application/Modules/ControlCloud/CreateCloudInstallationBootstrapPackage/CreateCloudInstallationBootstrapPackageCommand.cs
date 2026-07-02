namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.CreateCloudInstallationBootstrapPackage;

public sealed record CreateCloudInstallationBootstrapPackageCommand(
    Guid ClientId,
    string InstallationId,
    int ExpiresInHours,
    string CreatedBy,
    string DeploymentMode,
    string LocalServerVersion,
    string? SafarSuiteAppVersion = null,
    string? ClientDeploymentMode = null,
    string? SiteId = null,
    string? SiteRole = null,
    string? ParentSiteId = null,
    string? BranchCode = null,
    string? SyncTopologyId = null);
