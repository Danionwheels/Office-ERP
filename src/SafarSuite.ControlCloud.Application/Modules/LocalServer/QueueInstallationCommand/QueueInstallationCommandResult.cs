using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.QueueInstallationCommand;

public sealed class QueueInstallationCommandResult
{
    private QueueInstallationCommandResult(
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

    public static QueueInstallationCommandResult Success(ControlCloudInstallationCommand command)
    {
        return new QueueInstallationCommandResult(command, null, null);
    }

    public static QueueInstallationCommandResult Failure(string failureCode, string detail)
    {
        return new QueueInstallationCommandResult(null, failureCode, detail);
    }
}
