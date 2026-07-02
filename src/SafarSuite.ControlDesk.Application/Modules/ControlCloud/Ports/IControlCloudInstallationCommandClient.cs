using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;

public interface IControlCloudInstallationCommandClient
{
    Task<ControlCloudInstallationCommandClientResult> QueueCommandAsync(
        Guid clientId,
        string installationId,
        QueueInstallationCommandRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ControlCloudInstallationCommandClientResult
{
    private ControlCloudInstallationCommandClientResult(
        InstallationCommandResponse? command,
        string? failureCode,
        string? detail)
    {
        Command = command;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Command is not null;

    public InstallationCommandResponse? Command { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ControlCloudInstallationCommandClientResult Success(
        InstallationCommandResponse command)
    {
        return new ControlCloudInstallationCommandClientResult(
            command,
            failureCode: null,
            detail: null);
    }

    public static ControlCloudInstallationCommandClientResult Failure(
        string failureCode,
        string detail)
    {
        return new ControlCloudInstallationCommandClientResult(
            command: null,
            failureCode,
            detail);
    }
}
