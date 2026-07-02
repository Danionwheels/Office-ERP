namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.AcknowledgeInstallationCommand;

public sealed record AcknowledgeInstallationCommandCommand(
    string InstallationId,
    Guid CommandId,
    string ResultStatus,
    string? Detail,
    string PayloadJson);
