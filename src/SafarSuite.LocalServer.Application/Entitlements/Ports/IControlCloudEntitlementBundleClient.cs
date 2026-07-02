using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.LocalServer.Application.Entitlements.Ports;

public interface IControlCloudEntitlementBundleClient
{
    Task<ControlCloudEntitlementBundlePullResult> GetLatestBundleAsync(
        Guid clientId,
        string installationId,
        CancellationToken cancellationToken = default);
}

public sealed class ControlCloudEntitlementBundlePullResult
{
    private ControlCloudEntitlementBundlePullResult(
        ClientPortalSignedEntitlementBundleResponse? bundle,
        string? failureCode,
        string? detail)
    {
        Bundle = bundle;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Bundle is not null;

    public ClientPortalSignedEntitlementBundleResponse? Bundle { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ControlCloudEntitlementBundlePullResult Success(
        ClientPortalSignedEntitlementBundleResponse bundle)
    {
        return new ControlCloudEntitlementBundlePullResult(bundle, null, null);
    }

    public static ControlCloudEntitlementBundlePullResult Failure(
        string failureCode,
        string detail)
    {
        return new ControlCloudEntitlementBundlePullResult(null, failureCode, detail);
    }
}
