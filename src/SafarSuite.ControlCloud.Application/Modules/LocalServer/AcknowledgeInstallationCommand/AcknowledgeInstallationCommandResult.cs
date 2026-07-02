using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.AcknowledgeInstallationCommand;

public sealed class AcknowledgeInstallationCommandResult
{
    private AcknowledgeInstallationCommandResult(
        ControlCloudInstallationCommand? command,
        string? failureCode,
        string? detail)
    {
        Command = command;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Command is not null;

    public ControlCloudInstallationCommand? Command { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static AcknowledgeInstallationCommandResult Success(
        ControlCloudInstallationCommand command)
    {
        return new AcknowledgeInstallationCommandResult(command, null, null);
    }

    public static AcknowledgeInstallationCommandResult Failure(
        string failureCode,
        string detail)
    {
        return new AcknowledgeInstallationCommandResult(null, failureCode, detail);
    }
}
