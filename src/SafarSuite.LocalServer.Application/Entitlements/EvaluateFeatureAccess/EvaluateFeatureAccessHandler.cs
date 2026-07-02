using SafarSuite.LocalServer.Application.Common;
using SafarSuite.LocalServer.Application.Entitlements.Ports;
using SafarSuite.LocalServer.Domain.Entitlements;

namespace SafarSuite.LocalServer.Application.Entitlements.EvaluateFeatureAccess;

public sealed class EvaluateFeatureAccessHandler
{
    private static readonly TimeSpan ClockBackwardTolerance = TimeSpan.FromMinutes(5);

    private readonly ILocalServerEntitlementCache _cache;
    private readonly ILocalServerEntitlementTrustStateStore _trustStateStore;
    private readonly LocalServerEntitlementPolicy _policy;
    private readonly ILocalServerClock _clock;

    public EvaluateFeatureAccessHandler(
        ILocalServerEntitlementCache cache,
        ILocalServerEntitlementTrustStateStore trustStateStore,
        LocalServerEntitlementPolicy policy,
        ILocalServerClock clock)
    {
        _cache = cache;
        _trustStateStore = trustStateStore;
        _policy = policy;
        _clock = clock;
    }

    public async Task<LocalServerFeatureAccessDecision> HandleAsync(
        EvaluateFeatureAccessQuery query,
        CancellationToken cancellationToken = default)
    {
        var checkedAtUtc = _clock.UtcNow;
        await RecordLocalCheckAsync(
            query.ExpectedInstallationId,
            checkedAtUtc,
            cancellationToken);

        var entitlement = await _cache.GetCurrentAsync(cancellationToken);
        var asOfDate = query.AsOfDate
            ?? DateOnly.FromDateTime(checkedAtUtc.UtcDateTime);

        return _policy.EvaluateFeatureAccess(
            entitlement,
            query.ExpectedInstallationId,
            query.ModuleCode,
            asOfDate);
    }

    private async Task RecordLocalCheckAsync(
        string installationId,
        DateTimeOffset checkedAtUtc,
        CancellationToken cancellationToken)
    {
        var cleanInstallationId = installationId.Trim();

        if (cleanInstallationId.Length == 0)
        {
            return;
        }

        var trustState = await _trustStateStore.GetAsync(
                cleanInstallationId,
                cancellationToken)
            ?? LocalServerEntitlementTrustState.Empty(
                cleanInstallationId,
                checkedAtUtc);

        trustState = trustState.RecordLocalCheck(
            checkedAtUtc,
            ClockBackwardTolerance);
        await _trustStateStore.SaveAsync(trustState, cancellationToken);
    }
}
