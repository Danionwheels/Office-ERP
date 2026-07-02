using SafarSuite.LocalServer.Application.Common;
using SafarSuite.LocalServer.Application.Entitlements.Ports;

namespace SafarSuite.LocalServer.Application.Entitlements.ImportSignedEntitlementBundle;

public sealed class ImportSignedEntitlementBundleHandler
{
    private readonly ILocalServerEntitlementBundleVerifier _verifier;
    private readonly ILocalServerEntitlementCache _cache;
    private readonly ILocalServerClock _clock;

    public ImportSignedEntitlementBundleHandler(
        ILocalServerEntitlementBundleVerifier verifier,
        ILocalServerEntitlementCache cache,
        ILocalServerClock clock)
    {
        _verifier = verifier;
        _cache = cache;
        _clock = clock;
    }

    public async Task<ImportSignedEntitlementBundleResult> HandleAsync(
        ImportSignedEntitlementBundleCommand command,
        CancellationToken cancellationToken = default)
    {
        var expectedInstallationId = command.ExpectedInstallationId.Trim();

        if (expectedInstallationId.Length == 0)
        {
            return ImportSignedEntitlementBundleResult.Failure(
                "InstallationIdRequired",
                "Expected installation id is required before importing an entitlement bundle.");
        }

        var verification = _verifier.Verify(
            command.Bundle,
            expectedInstallationId,
            _clock.UtcNow);

        if (!verification.IsValid)
        {
            return ImportSignedEntitlementBundleResult.Failure(
                verification.FailureCode ?? "EntitlementBundleInvalid",
                verification.Detail ?? "Entitlement bundle verification failed.");
        }

        var incomingEntitlement = verification.Entitlement!;
        var cachedEntitlement = await _cache.GetCurrentAsync(cancellationToken);

        if (cachedEntitlement is not null
            && cachedEntitlement.InstallationId == expectedInstallationId
            && incomingEntitlement.EntitlementVersion < cachedEntitlement.EntitlementVersion)
        {
            return ImportSignedEntitlementBundleResult.Failure(
                "EntitlementVersionRejected",
                "Incoming entitlement bundle is older than the cached entitlement.");
        }

        await _cache.SaveAsync(incomingEntitlement, cancellationToken);

        return ImportSignedEntitlementBundleResult.Success(incomingEntitlement);
    }
}
