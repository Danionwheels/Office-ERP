using SafarSuite.LocalServer.Application.Entitlements.PullEntitlementFromControlCloud;
using SafarSuite.LocalServer.Application.Registration.Ports;

namespace SafarSuite.LocalServer.Application.Entitlements.PullEntitlementFromBootstrapConfiguration;

public sealed class PullEntitlementFromBootstrapConfigurationHandler
{
    private readonly ILocalServerBootstrapConfigurationStore _configurationStore;
    private readonly PullEntitlementFromControlCloudHandler _pullHandler;

    public PullEntitlementFromBootstrapConfigurationHandler(
        ILocalServerBootstrapConfigurationStore configurationStore,
        PullEntitlementFromControlCloudHandler pullHandler)
    {
        _configurationStore = configurationStore;
        _pullHandler = pullHandler;
    }

    public async Task<PullEntitlementFromControlCloudResult> HandleAsync(
        PullEntitlementFromBootstrapConfigurationCommand command,
        CancellationToken cancellationToken = default)
    {
        var configuration = await _configurationStore.GetCurrentAsync(cancellationToken);

        if (configuration is null)
        {
            return PullEntitlementFromControlCloudResult.Failure(
                "BootstrapConfigurationMissing",
                "A verified bootstrap configuration is required before pulling entitlement from Control Cloud.");
        }

        return await _pullHandler.HandleAsync(
            new PullEntitlementFromControlCloudCommand(
                configuration.ClientId,
                configuration.InstallationId),
            cancellationToken);
    }
}
