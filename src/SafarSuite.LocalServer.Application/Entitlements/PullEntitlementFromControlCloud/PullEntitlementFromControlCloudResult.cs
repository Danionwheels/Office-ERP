using SafarSuite.LocalServer.Domain.Entitlements;

namespace SafarSuite.LocalServer.Application.Entitlements.PullEntitlementFromControlCloud;

public sealed class PullEntitlementFromControlCloudResult
{
    private PullEntitlementFromControlCloudResult(
        LocalServerCachedEntitlement? entitlement,
        DateTimeOffset? pulledAtUtc,
        string? failureCode,
        string? detail)
    {
        Entitlement = entitlement;
        PulledAtUtc = pulledAtUtc;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Entitlement is not null;

    public LocalServerCachedEntitlement? Entitlement { get; }

    public DateTimeOffset? PulledAtUtc { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static PullEntitlementFromControlCloudResult Success(
        LocalServerCachedEntitlement entitlement,
        DateTimeOffset pulledAtUtc)
    {
        return new PullEntitlementFromControlCloudResult(
            entitlement,
            pulledAtUtc,
            null,
            null);
    }

    public static PullEntitlementFromControlCloudResult Failure(
        string failureCode,
        string detail)
    {
        return new PullEntitlementFromControlCloudResult(
            null,
            null,
            failureCode,
            detail);
    }
}
