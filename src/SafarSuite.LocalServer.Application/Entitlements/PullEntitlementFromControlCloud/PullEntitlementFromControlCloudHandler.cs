using SafarSuite.LocalServer.Application.Common;
using SafarSuite.LocalServer.Application.Entitlements.ImportSignedEntitlementBundle;
using SafarSuite.LocalServer.Application.Entitlements.Ports;
using SafarSuite.LocalServer.Domain.Entitlements;

namespace SafarSuite.LocalServer.Application.Entitlements.PullEntitlementFromControlCloud;

public sealed class PullEntitlementFromControlCloudHandler
{
    private readonly IControlCloudEntitlementBundleClient _cloudClient;
    private readonly ImportSignedEntitlementBundleHandler _importHandler;
    private readonly ILocalServerClock _clock;

    public PullEntitlementFromControlCloudHandler(
        IControlCloudEntitlementBundleClient cloudClient,
        ImportSignedEntitlementBundleHandler importHandler,
        ILocalServerClock clock)
    {
        _cloudClient = cloudClient;
        _importHandler = importHandler;
        _clock = clock;
    }

    public async Task<PullEntitlementFromControlCloudResult> HandleAsync(
        PullEntitlementFromControlCloudCommand command,
        CancellationToken cancellationToken = default)
    {
        var installationId = command.InstallationId.Trim();

        if (command.ClientId == Guid.Empty)
        {
            return PullEntitlementFromControlCloudResult.Failure(
                "ClientIdRequired",
                "Client id is required before pulling an entitlement bundle.");
        }

        if (installationId.Length == 0)
        {
            return PullEntitlementFromControlCloudResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before pulling an entitlement bundle.");
        }

        var pulledAtUtc = _clock.UtcNow;
        var pullResult = await _cloudClient.GetLatestBundleAsync(
            command.ClientId,
            installationId,
            cancellationToken);

        if (!pullResult.IsSuccess)
        {
            return PullEntitlementFromControlCloudResult.Failure(
                pullResult.FailureCode ?? "ControlCloudPullFailed",
                pullResult.Detail ?? "Control Cloud entitlement bundle pull failed.");
        }

        var importResult = await _importHandler.HandleAsync(
            new ImportSignedEntitlementBundleCommand(
                installationId,
                pullResult.Bundle!,
                LocalServerEntitlementImportSources.ControlCloudPull),
            cancellationToken);

        if (!importResult.IsSuccess)
        {
            return PullEntitlementFromControlCloudResult.Failure(
                importResult.FailureCode ?? "EntitlementImportFailed",
                importResult.Detail ?? "Pulled entitlement bundle could not be imported.");
        }

        return PullEntitlementFromControlCloudResult.Success(
            importResult.Entitlement!,
            pulledAtUtc);
    }
}
