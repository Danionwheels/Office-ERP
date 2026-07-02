using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.ReportInstallationHeartbeat;

public sealed class ReportInstallationHeartbeatResult
{
    private ReportInstallationHeartbeatResult(
        ControlCloudInstallationHeartbeat? heartbeat,
        string? failureCode,
        string? detail)
    {
        Heartbeat = heartbeat;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Heartbeat is not null;

    public ControlCloudInstallationHeartbeat? Heartbeat { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ReportInstallationHeartbeatResult Success(
        ControlCloudInstallationHeartbeat heartbeat)
    {
        return new ReportInstallationHeartbeatResult(
            heartbeat,
            failureCode: null,
            detail: null);
    }

    public static ReportInstallationHeartbeatResult Failure(
        string failureCode,
        string detail)
    {
        return new ReportInstallationHeartbeatResult(
            heartbeat: null,
            failureCode,
            detail);
    }
}
