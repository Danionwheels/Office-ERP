using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.LocalServer.Application.Heartbeats.Ports;

public interface IControlCloudHeartbeatClient
{
    Task<ControlCloudHeartbeatReportResult> ReportHeartbeatAsync(
        string installationId,
        ReportLocalServerHeartbeatRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ControlCloudHeartbeatReportResult
{
    private ControlCloudHeartbeatReportResult(
        LocalServerHeartbeatResponse? heartbeat,
        string? failureCode,
        string? detail)
    {
        Heartbeat = heartbeat;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Heartbeat is not null;

    public LocalServerHeartbeatResponse? Heartbeat { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ControlCloudHeartbeatReportResult Success(
        LocalServerHeartbeatResponse heartbeat)
    {
        return new ControlCloudHeartbeatReportResult(
            heartbeat,
            failureCode: null,
            detail: null);
    }

    public static ControlCloudHeartbeatReportResult Failure(
        string failureCode,
        string detail)
    {
        return new ControlCloudHeartbeatReportResult(
            heartbeat: null,
            failureCode,
            detail);
    }
}
