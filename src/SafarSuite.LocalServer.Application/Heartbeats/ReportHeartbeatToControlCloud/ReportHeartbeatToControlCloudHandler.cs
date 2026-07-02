using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Common;
using SafarSuite.LocalServer.Application.Entitlements.Ports;
using SafarSuite.LocalServer.Application.Heartbeats.Ports;
using SafarSuite.LocalServer.Domain.Entitlements;

namespace SafarSuite.LocalServer.Application.Heartbeats.ReportHeartbeatToControlCloud;

public sealed class ReportHeartbeatToControlCloudHandler
{
    private readonly IControlCloudHeartbeatClient _cloudClient;
    private readonly ILocalServerEntitlementCache _cache;
    private readonly LocalServerEntitlementPolicy _policy;
    private readonly ILocalServerClock _clock;

    public ReportHeartbeatToControlCloudHandler(
        IControlCloudHeartbeatClient cloudClient,
        ILocalServerEntitlementCache cache,
        LocalServerEntitlementPolicy policy,
        ILocalServerClock clock)
    {
        _cloudClient = cloudClient;
        _cache = cache;
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
            command.Detail);
        var report = await _cloudClient.ReportHeartbeatAsync(
            installationId,
            request,
            cancellationToken);

        if (!report.IsSuccess)
        {
            return ReportHeartbeatToControlCloudResult.Failure(
                entitlementState,
                report.FailureCode ?? "ControlCloudHeartbeatFailed",
                report.Detail ?? "Control Cloud did not accept the heartbeat.");
        }

        return ReportHeartbeatToControlCloudResult.Success(
            report.Heartbeat!,
            entitlementState);
    }

    private static string? NormalizeRequiredText(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
