using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Domain.Entitlements;

namespace SafarSuite.LocalServer.Application.Entitlements.Ports;

public interface ILocalServerEntitlementBundleVerifier
{
    LocalServerEntitlementBundleVerificationResult Verify(
        ClientPortalSignedEntitlementBundleResponse bundle,
        string expectedInstallationId,
        DateTimeOffset importedAtUtc);
}

public sealed class LocalServerEntitlementBundleVerificationResult
{
    private LocalServerEntitlementBundleVerificationResult(
        LocalServerCachedEntitlement? entitlement,
        string? failureCode,
        string? detail)
    {
        Entitlement = entitlement;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsValid => Entitlement is not null;

    public LocalServerCachedEntitlement? Entitlement { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static LocalServerEntitlementBundleVerificationResult Success(
        LocalServerCachedEntitlement entitlement)
    {
        return new LocalServerEntitlementBundleVerificationResult(entitlement, null, null);
    }

    public static LocalServerEntitlementBundleVerificationResult Failure(
        string failureCode,
        string detail)
    {
        return new LocalServerEntitlementBundleVerificationResult(null, failureCode, detail);
    }
}
