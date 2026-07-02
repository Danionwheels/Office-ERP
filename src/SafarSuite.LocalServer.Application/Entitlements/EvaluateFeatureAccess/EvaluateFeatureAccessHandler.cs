using SafarSuite.LocalServer.Application.Common;
using SafarSuite.LocalServer.Application.Entitlements.Ports;
using SafarSuite.LocalServer.Domain.Entitlements;

namespace SafarSuite.LocalServer.Application.Entitlements.EvaluateFeatureAccess;

public sealed class EvaluateFeatureAccessHandler
{
    private readonly ILocalServerEntitlementCache _cache;
    private readonly LocalServerEntitlementPolicy _policy;
    private readonly ILocalServerClock _clock;

    public EvaluateFeatureAccessHandler(
        ILocalServerEntitlementCache cache,
        LocalServerEntitlementPolicy policy,
        ILocalServerClock clock)
    {
        _cache = cache;
        _policy = policy;
        _clock = clock;
    }

    public async Task<LocalServerFeatureAccessDecision> HandleAsync(
        EvaluateFeatureAccessQuery query,
        CancellationToken cancellationToken = default)
    {
        var entitlement = await _cache.GetCurrentAsync(cancellationToken);
        var asOfDate = query.AsOfDate
            ?? DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);

        return _policy.EvaluateFeatureAccess(
            entitlement,
            query.ExpectedInstallationId,
            query.ModuleCode,
            asOfDate);
    }
}
