using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.LocalServer.Application.Commands.Ports;

public interface IControlCloudInstallationCommandClient
{
    Task<ControlCloudPendingInstallationCommandsResult> GetPendingAsync(
        string installationId,
        CancellationToken cancellationToken = default);

    Task<ControlCloudInstallationCommandAcknowledgementResult> AcknowledgeAsync(
        string installationId,
        Guid commandId,
        AcknowledgeInstallationCommandRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record ControlCloudPendingInstallationCommandsResult(
    PendingInstallationCommandsResponse? Response,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => Response is not null;

    public static ControlCloudPendingInstallationCommandsResult Success(
        PendingInstallationCommandsResponse response)
    {
        return new ControlCloudPendingInstallationCommandsResult(
            response,
            FailureCode: null,
            Detail: null);
    }

    public static ControlCloudPendingInstallationCommandsResult Failure(
        string failureCode,
        string detail)
    {
        return new ControlCloudPendingInstallationCommandsResult(
            Response: null,
            failureCode,
            detail);
    }
}

public sealed record ControlCloudInstallationCommandAcknowledgementResult(
    InstallationCommandResponse? Command,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => Command is not null;

    public static ControlCloudInstallationCommandAcknowledgementResult Success(
        InstallationCommandResponse command)
    {
        return new ControlCloudInstallationCommandAcknowledgementResult(
            command,
            FailureCode: null,
            Detail: null);
    }

    public static ControlCloudInstallationCommandAcknowledgementResult Failure(
        string failureCode,
        string detail)
    {
        return new ControlCloudInstallationCommandAcknowledgementResult(
            Command: null,
            failureCode,
            detail);
    }
}
