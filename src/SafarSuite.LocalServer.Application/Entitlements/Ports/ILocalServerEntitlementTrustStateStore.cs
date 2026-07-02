using SafarSuite.LocalServer.Domain.Entitlements;

namespace SafarSuite.LocalServer.Application.Entitlements.Ports;

public interface ILocalServerEntitlementTrustStateStore
{
    Task<LocalServerEntitlementTrustState?> GetAsync(
        string installationId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        LocalServerEntitlementTrustState state,
        CancellationToken cancellationToken = default);
}
