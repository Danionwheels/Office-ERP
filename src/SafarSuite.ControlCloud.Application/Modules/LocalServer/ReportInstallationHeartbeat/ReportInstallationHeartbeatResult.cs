using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.ReportInstallationHeartbeat;

public sealed class ReportInstallationHeartbeatResult
{
    private ReportInstallationHeartbeatResult(
        ControlCloudInstallationHeartbeat? heartbeat,
        ControlCloudInstallationDeploymentProfile? deploymentProfile,
        string? failureCode,
        string? detail)
    {
        Heartbeat = heartbeat;
        DeploymentProfile = deploymentProfile;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Heartbeat is not null;

    public ControlCloudInstallationHeartbeat? Heartbeat { get; }

    public ControlCloudInstallationDeploymentProfile? DeploymentProfile { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ReportInstallationHeartbeatResult Success(
        ControlCloudInstallationHeartbeat heartbeat,
        ControlCloudInstallationDeploymentProfile deploymentProfile)
    {
        return new ReportInstallationHeartbeatResult(
            heartbeat,
            deploymentProfile,
            failureCode: null,
            detail: null);
    }

    public static ReportInstallationHeartbeatResult Failure(
        string failureCode,
        string detail)
    {
        return new ReportInstallationHeartbeatResult(
            heartbeat: null,
            deploymentProfile: null,
            failureCode,
            detail);
    }
}
