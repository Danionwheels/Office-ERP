using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.GetPendingInstallationCommands;

public sealed class GetPendingInstallationCommandsResult
{
    private GetPendingInstallationCommandsResult(
        IReadOnlyCollection<ControlCloudInstallationCommand>? commands,
        string? failureCode,
        string? detail)
    {
        Commands = commands;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Commands is not null;

    public IReadOnlyCollection<ControlCloudInstallationCommand>? Commands { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static GetPendingInstallationCommandsResult Success(
        IReadOnlyCollection<ControlCloudInstallationCommand> commands)
    {
        return new GetPendingInstallationCommandsResult(commands, null, null);
    }

    public static GetPendingInstallationCommandsResult Failure(
        string failureCode,
        string detail)
    {
        return new GetPendingInstallationCommandsResult(null, failureCode, detail);
    }
}
