using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Commands.ProcessInstallationCommands;
using SafarSuite.LocalServer.Application.Registration.Ports;

namespace SafarSuite.LocalServer.Application.Commands.ProcessInstallationCommandsFromBootstrapConfiguration;

public sealed class ProcessInstallationCommandsFromBootstrapConfigurationHandler
{
    private readonly ILocalServerBootstrapConfigurationStore _configurationStore;
    private readonly ProcessInstallationCommandsHandler _commandHandler;

    public ProcessInstallationCommandsFromBootstrapConfigurationHandler(
        ILocalServerBootstrapConfigurationStore configurationStore,
        ProcessInstallationCommandsHandler commandHandler)
    {
        _configurationStore = configurationStore;
        _commandHandler = commandHandler;
    }

    public async Task<ProcessInstallationCommandsResult> HandleAsync(
        ProcessInstallationCommandsFromBootstrapConfigurationCommand command,
        CancellationToken cancellationToken = default)
    {
        var configuration = await _configurationStore.GetCurrentAsync(cancellationToken);

        if (configuration is null)
        {
            return ProcessInstallationCommandsResult.Failure(
                "BootstrapConfigurationMissing",
                "A verified bootstrap configuration is required before processing Control Cloud commands.");
        }

        return await _commandHandler.HandleAsync(
            new ProcessInstallationCommandsCommand(
                configuration.ClientId,
                configuration.InstallationId,
                configuration.LocalServerVersion,
                new LocalServerDeploymentProfileResponse(
                    configuration.DeploymentProfile.BootstrapMode,
                    configuration.DeploymentProfile.ClientDeploymentMode,
                    configuration.DeploymentProfile.SiteId,
                    configuration.DeploymentProfile.SiteRole,
                    configuration.DeploymentProfile.ParentSiteId,
                    configuration.DeploymentProfile.BranchCode,
                    configuration.DeploymentProfile.SyncTopologyId)),
            cancellationToken);
    }
}
