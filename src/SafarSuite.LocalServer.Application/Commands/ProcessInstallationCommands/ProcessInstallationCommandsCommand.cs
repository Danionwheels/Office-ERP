using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.LocalServer.Application.Commands.ProcessInstallationCommands;

public sealed record ProcessInstallationCommandsCommand(
    Guid ClientId,
    string InstallationId,
    string LocalServerVersion,
    LocalServerDeploymentProfileResponse? DeploymentProfile = null);
