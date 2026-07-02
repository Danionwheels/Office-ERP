using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed record GetClientPortalSignedEntitlementBundleResult(
    ControlCloudSignedEntitlementBundle? Bundle,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => Bundle is not null;

    public static GetClientPortalSignedEntitlementBundleResult Success(
        ControlCloudSignedEntitlementBundle bundle)
    {
        return new GetClientPortalSignedEntitlementBundleResult(
            bundle,
            FailureCode: null,
            Detail: null);
    }

    public static GetClientPortalSignedEntitlementBundleResult Failure(
        string failureCode,
        string detail)
    {
        return new GetClientPortalSignedEntitlementBundleResult(
            Bundle: null,
            failureCode,
            detail);
    }
}
