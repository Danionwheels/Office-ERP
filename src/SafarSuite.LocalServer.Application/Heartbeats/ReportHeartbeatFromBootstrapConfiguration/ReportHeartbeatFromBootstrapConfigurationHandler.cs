using SafarSuite.LocalServer.Application.Heartbeats.Ports;
using SafarSuite.LocalServer.Application.Heartbeats.ReportHeartbeatToControlCloud;
using SafarSuite.LocalServer.Application.Registration.Ports;

namespace SafarSuite.LocalServer.Application.Heartbeats.ReportHeartbeatFromBootstrapConfiguration;

public sealed class ReportHeartbeatFromBootstrapConfigurationHandler
{
    private readonly ILocalServerBootstrapConfigurationStore _configurationStore;
    private readonly ReportHeartbeatToControlCloudHandler _heartbeatHandler;
    private readonly ILocalServerHeartbeatPairingStatusProvider? _pairingStatusProvider;

    public ReportHeartbeatFromBootstrapConfigurationHandler(
        ILocalServerBootstrapConfigurationStore configurationStore,
        ReportHeartbeatToControlCloudHandler heartbeatHandler,
        ILocalServerHeartbeatPairingStatusProvider? pairingStatusProvider = null)
    {
        _configurationStore = configurationStore;
        _heartbeatHandler = heartbeatHandler;
        _pairingStatusProvider = pairingStatusProvider;
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

        var pairingStatus = _pairingStatusProvider is null
            ? null
            : await _pairingStatusProvider.GetCurrentAsync(
                configuration.ClientId,
                configuration.InstallationId,
                cancellationToken);

        return await _heartbeatHandler.HandleAsync(
            new ReportHeartbeatToControlCloudCommand(
                configuration.ClientId,
                configuration.InstallationId,
                configuration.LocalServerVersion,
                command.AsOfDate,
                command.Detail,
                pairingStatus),
            cancellationToken);
    }
}
