using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Domain.Entitlements;

namespace SafarSuite.LocalServer.Application.Heartbeats.ReportHeartbeatToControlCloud;

public sealed class ReportHeartbeatToControlCloudResult
{
    private ReportHeartbeatToControlCloudResult(
        LocalServerHeartbeatResponse? heartbeat,
        LocalServerEntitlementStateDecision? entitlementState,
        string? failureCode,
        string? detail)
    {
        Heartbeat = heartbeat;
        EntitlementState = entitlementState;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Heartbeat is not null;

    public LocalServerHeartbeatResponse? Heartbeat { get; }

    public LocalServerEntitlementStateDecision? EntitlementState { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ReportHeartbeatToControlCloudResult Success(
        LocalServerHeartbeatResponse heartbeat,
        LocalServerEntitlementStateDecision entitlementState)
    {
        return new ReportHeartbeatToControlCloudResult(
            heartbeat,
            entitlementState,
            failureCode: null,
            detail: null);
    }

    public static ReportHeartbeatToControlCloudResult Failure(
        LocalServerEntitlementStateDecision? entitlementState,
        string failureCode,
        string detail)
    {
        return new ReportHeartbeatToControlCloudResult(
            heartbeat: null,
            entitlementState,
            failureCode,
            detail);
    }
}
