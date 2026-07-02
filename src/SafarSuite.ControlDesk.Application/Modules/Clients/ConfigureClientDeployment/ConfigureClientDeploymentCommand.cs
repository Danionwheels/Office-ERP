namespace SafarSuite.ControlDesk.Application.Modules.Clients.ConfigureClientDeployment;

public sealed record ConfigureClientDeploymentCommand(
    Guid ClientId,
    string InstallationId,
    string DisplayName,
    string BootstrapMode,
    string ClientDeploymentMode,
    string SiteId,
    string SiteRole,
    string? ParentSiteId,
    string? BranchCode,
    string? SyncTopologyId,
    string LocalServerVersion,
    string? SafarSuiteAppVersion,
    bool IsPrimary);
