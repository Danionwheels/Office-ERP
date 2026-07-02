using SafarSuite.LocalServer.Application.Commands;

namespace SafarSuite.LocalServer.Application.Commands.ProcessInstallationCommands;

public sealed record ProcessInstallationCommandsResult(
    bool IsSuccess,
    int PendingCommandCount,
    int AppliedCount,
    int FailedCount,
    int RejectedCount,
    IReadOnlyCollection<ProcessedInstallationCommandResult> Commands,
    string? FailureCode,
    string? Detail)
{
    public static ProcessInstallationCommandsResult Success(
        int pendingCommandCount,
        IReadOnlyCollection<ProcessedInstallationCommandResult> commands)
    {
        return new ProcessInstallationCommandsResult(
            IsSuccess: true,
            pendingCommandCount,
            commands.Count(command => command.ResultStatus == LocalServerInstallationCommandAcknowledgementStatuses.Applied),
            commands.Count(command => command.ResultStatus == LocalServerInstallationCommandAcknowledgementStatuses.Failed),
            commands.Count(command => command.ResultStatus == LocalServerInstallationCommandAcknowledgementStatuses.Rejected),
            commands,
            FailureCode: null,
            Detail: null);
    }

    public static ProcessInstallationCommandsResult Failure(
        string failureCode,
        string detail)
    {
        return new ProcessInstallationCommandsResult(
            IsSuccess: false,
            PendingCommandCount: 0,
            AppliedCount: 0,
            FailedCount: 0,
            RejectedCount: 0,
            Commands: Array.Empty<ProcessedInstallationCommandResult>(),
            failureCode,
            detail);
    }
}

public sealed record ProcessedInstallationCommandResult(
    Guid CommandId,
    long CommandVersion,
    string CommandType,
    string ResultStatus,
    bool Acknowledged,
    string? FailureCode,
    string Detail,
    Guid? DiagnosticReportId = null,
    long? EntitlementVersion = null);
