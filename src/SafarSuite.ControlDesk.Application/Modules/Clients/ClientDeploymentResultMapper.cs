using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients;

internal static class ClientDeploymentResultMapper
{
    public static ClientDeploymentResult ToResult(ClientDeployment deployment)
    {
        return new ClientDeploymentResult(
            deployment.Id.Value,
            deployment.ClientId.Value,
            deployment.DisplayName,
            deployment.InstallationId,
            deployment.BootstrapMode,
            deployment.ClientDeploymentMode,
            deployment.SiteId,
            deployment.SiteRole,
            deployment.ParentSiteId,
            deployment.BranchCode,
            deployment.SyncTopologyId,
            deployment.LocalServerVersion,
            deployment.SafarSuiteAppVersion,
            deployment.IsPrimary,
            deployment.CreatedAtUtc,
            deployment.UpdatedAtUtc);
    }
}
