using SafarSuite.LocalServer.Domain.Entitlements;

namespace SafarSuite.LocalServer.Application.Entitlements.Ports;

public interface ILocalServerEntitlementCache
{
    Task<LocalServerCachedEntitlement?> GetCurrentAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        LocalServerCachedEntitlement entitlement,
        CancellationToken cancellationToken = default);
}
