namespace SafarSuite.ControlDesk.Application.Modules.Clients;

public sealed record ClientDeploymentResult(
    Guid ClientDeploymentId,
    Guid ClientId,
    string DisplayName,
    string InstallationId,
    string BootstrapMode,
    string ClientDeploymentMode,
    string SiteId,
    string SiteRole,
    string? ParentSiteId,
    string? BranchCode,
    string? SyncTopologyId,
    string LocalServerVersion,
    string SafarSuiteAppVersion,
    bool IsPrimary,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
