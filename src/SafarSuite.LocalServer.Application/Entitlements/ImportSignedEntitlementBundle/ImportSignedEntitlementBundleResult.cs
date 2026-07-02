using SafarSuite.LocalServer.Domain.Entitlements;

namespace SafarSuite.LocalServer.Application.Entitlements.ImportSignedEntitlementBundle;

public sealed class ImportSignedEntitlementBundleResult
{
    private ImportSignedEntitlementBundleResult(
        LocalServerCachedEntitlement? entitlement,
        string? failureCode,
        string? detail)
    {
        Entitlement = entitlement;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Entitlement is not null;

    public LocalServerCachedEntitlement? Entitlement { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ImportSignedEntitlementBundleResult Success(
        LocalServerCachedEntitlement entitlement)
    {
        return new ImportSignedEntitlementBundleResult(entitlement, null, null);
    }

    public static ImportSignedEntitlementBundleResult Failure(
        string failureCode,
        string detail)
    {
        return new ImportSignedEntitlementBundleResult(null, failureCode, detail);
    }
}
