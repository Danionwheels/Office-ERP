using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Common;
using SafarSuite.LocalServer.Application.Entitlements.Ports;
using SafarSuite.LocalServer.Application.Heartbeats.Ports;
using SafarSuite.LocalServer.Domain.Entitlements;

namespace SafarSuite.LocalServer.Application.Heartbeats.ReportHeartbeatToControlCloud;

public sealed class ReportHeartbeatToControlCloudHandler
{
    private static readonly TimeSpan ClockBackwardTolerance = TimeSpan.FromMinutes(5);

    private readonly IControlCloudHeartbeatClient _cloudClient;
    private readonly ILocalServerEntitlementCache _cache;
    private readonly ILocalServerEntitlementTrustStateStore _trustStateStore;
    private readonly LocalServerEntitlementPolicy _policy;
    private readonly ILocalServerClock _clock;

    public ReportHeartbeatToControlCloudHandler(
        IControlCloudHeartbeatClient cloudClient,
        ILocalServerEntitlementCache cache,
        ILocalServerEntitlementTrustStateStore trustStateStore,
        LocalServerEntitlementPolicy policy,
        ILocalServerClock clock)
    {
        _cloudClient = cloudClient;
        _cache = cache;
        _trustStateStore = trustStateStore;
        _policy = policy;
        _clock = clock;
    }

    public async Task<ReportHeartbeatToControlCloudResult> HandleAsync(
        ReportHeartbeatToControlCloudCommand command,
        CancellationToken cancellationToken = default)
    {
        var installationId = NormalizeRequiredText(command.InstallationId);

        if (installationId is null)
        {
            return ReportHeartbeatToControlCloudResult.Failure(
                entitlementState: null,
                "InstallationIdRequired",
                "Installation id is required before reporting heartbeat.");
        }

        if (command.ClientId == Guid.Empty)
        {
            return ReportHeartbeatToControlCloudResult.Failure(
                entitlementState: null,
                "ClientIdRequired",
                "Client id is required before reporting heartbeat.");
        }

        var reportedAtUtc = _clock.UtcNow;
        var trustState = await LoadTrustStateAsync(
            installationId,
            reportedAtUtc,
            cancellationToken);
        trustState = trustState.RecordLocalCheck(
            reportedAtUtc,
            ClockBackwardTolerance);
        var asOfDate = command.AsOfDate
            ?? DateOnly.FromDateTime(reportedAtUtc.UtcDateTime);
        var entitlement = await _cache.GetCurrentAsync(cancellationToken);
        var entitlementState = _policy.EvaluateEntitlementState(
            entitlement,
            installationId,
            asOfDate);
        var request = new ReportLocalServerHeartbeatRequest(
            command.ClientId,
            NormalizeRequiredText(command.LocalServerVersion) ?? "Unknown",
            reportedAtUtc,
            entitlementState.AccessState,
            entitlementState.EntitlementVersion,
            entitlementState.PaidUntil,
            entitlementState.WarningStartsAt,
            entitlementState.GraceUntil,
            entitlementState.OfflineValidUntil,
            command.Detail,
            PairingStatus: command.PairingStatus,
            EntitlementState: ToEntitlementState(entitlement));
        var report = await _cloudClient.ReportHeartbeatAsync(
            installationId,
            request,
            cancellationToken);

        if (!report.IsSuccess)
        {
            await _trustStateStore.SaveAsync(trustState, cancellationToken);

            return ReportHeartbeatToControlCloudResult.Failure(
                entitlementState,
                report.FailureCode ?? "ControlCloudHeartbeatFailed",
                report.Detail ?? "Control Cloud did not accept the heartbeat.");
        }

        trustState = trustState.RecordSuccessfulCloudTime(
            report.Heartbeat!.ReceivedAtUtc,
            reportedAtUtc);
        await _trustStateStore.SaveAsync(trustState, cancellationToken);

        return ReportHeartbeatToControlCloudResult.Success(
            report.Heartbeat,
            entitlementState);
    }

    private async Task<LocalServerEntitlementTrustState> LoadTrustStateAsync(
        string installationId,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken)
    {
        return await _trustStateStore.GetAsync(installationId, cancellationToken)
            ?? LocalServerEntitlementTrustState.Empty(installationId, createdAtUtc);
    }

    private static string? NormalizeRequiredText(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static ControlCloudEntitlementStateValuesResponse? ToEntitlementState(
        LocalServerCachedEntitlement? entitlement)
    {
        if (entitlement is null)
        {
            return null;
        }

        return new ControlCloudEntitlementStateValuesResponse(
            entitlement.EntitlementVersion,
            entitlement.EffectiveFromUtc
            ?? new DateTimeOffset(entitlement.ValidFrom.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            entitlement.Status,
            entitlement.PaidUntil,
            entitlement.WarningStartsAt,
            entitlement.GraceUntil,
            entitlement.OfflineValidUntil,
            entitlement.AllowedDevices,
            entitlement.AllowedBranches,
            entitlement.AllowedNamedUsers,
            entitlement.AllowedConcurrentUsers,
            entitlement.Modules.Select(module => new ControlCloudEntitlementStateModuleResponse(
                module.ModuleCode,
                module.IsEnabled)).ToArray(),
            (entitlement.FeatureLimits ?? []).Select(limit =>
                new ControlCloudEntitlementStateFeatureLimitResponse(
                    limit.ModuleCode,
                    limit.FeatureCode,
                    limit.LimitValue,
                    limit.Unit)).ToArray());
    }
}
