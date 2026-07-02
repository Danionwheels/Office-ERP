using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

public interface IControlCloudEntitlementBundleIssueRepository
{
    Task AddAsync(
        ControlCloudEntitlementBundleIssue issue,
        CancellationToken cancellationToken = default);

    Task<ControlCloudEntitlementBundleIssue?> GetLatestByInstallationIdAsync(
        string installationId,
        CancellationToken cancellationToken = default);
}
