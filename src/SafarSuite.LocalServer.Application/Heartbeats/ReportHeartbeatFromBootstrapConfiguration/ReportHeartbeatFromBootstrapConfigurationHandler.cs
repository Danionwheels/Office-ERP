using SafarSuite.LocalServer.Application.Heartbeats.ReportHeartbeatToControlCloud;
using SafarSuite.LocalServer.Application.Registration.Ports;

namespace SafarSuite.LocalServer.Application.Heartbeats.ReportHeartbeatFromBootstrapConfiguration;

public sealed class ReportHeartbeatFromBootstrapConfigurationHandler
{
    private readonly ILocalServerBootstrapConfigurationStore _configurationStore;
    private readonly ReportHeartbeatToControlCloudHandler _heartbeatHandler;

    public ReportHeartbeatFromBootstrapConfigurationHandler(
        ILocalServerBootstrapConfigurationStore configurationStore,
        ReportHeartbeatToControlCloudHandler heartbeatHandler)
    {
        _configurationStore = configurationStore;
        _heartbeatHandler = heartbeatHandler;
    }

    public async Task<ReportHeartbeatToControlCloudResult> HandleAsync(
        ReportHeartbeatFromBootstrapConfigurationCommand command,
        CancellationToken cancellationToken = default)
    {
        var configuration = await _configurationStore.GetCurrentAsync(cancellationToken);

        if (configuration is null)
        {
            return ReportHeartbeatToControlCloudResult.Failure(
                entitlementState: null,
                "BootstrapConfigurationMissing",
                "A verified bootstrap configuration is required before reporting heartbeat.");
        }

        return await _heartbeatHandler.HandleAsync(
            new ReportHeartbeatToControlCloudCommand(
                configuration.ClientId,
                configuration.InstallationId,
                configuration.LocalServerVersion,
                command.AsOfDate,
                command.Detail),
            cancellationToken);
    }
}
