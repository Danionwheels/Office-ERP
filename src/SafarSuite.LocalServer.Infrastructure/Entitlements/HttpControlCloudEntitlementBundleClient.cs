using System.Net;
using System.Net.Http.Json;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Entitlements.Ports;

namespace SafarSuite.LocalServer.Infrastructure.Entitlements;

public sealed class HttpControlCloudEntitlementBundleClient
    : IControlCloudEntitlementBundleClient
{
    private readonly HttpClient _httpClient;
    private readonly ControlCloudEntitlementPullOptions _options;

    public HttpControlCloudEntitlementBundleClient(
        HttpClient httpClient,
        ControlCloudEntitlementPullOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<ControlCloudEntitlementBundlePullResult> GetLatestBundleAsync(
        Guid clientId,
        string installationId,
        CancellationToken cancellationToken = default)
    {
        var cleanInstallationId = installationId.Trim();

        if (clientId == Guid.Empty)
        {
            return ControlCloudEntitlementBundlePullResult.Failure(
                "ClientIdRequired",
                "Client id is required before pulling a Control Cloud entitlement bundle.");
        }

        if (cleanInstallationId.Length == 0)
        {
            return ControlCloudEntitlementBundlePullResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before pulling a Control Cloud entitlement bundle.");
        }

        var requestUri = BuildRequestUri(clientId, cleanInstallationId);

        try
        {
            using var response = await _httpClient.GetAsync(
                requestUri,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return ControlCloudEntitlementBundlePullResult.Failure(
                    "EntitlementNotFound",
                    "Control Cloud did not return an entitlement bundle for this client.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return ControlCloudEntitlementBundlePullResult.Failure(
                    "ControlCloudPullFailed",
                    $"Control Cloud returned HTTP {(int)response.StatusCode}.");
            }

            var bundle = await response.Content.ReadFromJsonAsync<ClientPortalSignedEntitlementBundleResponse>(
                cancellationToken);

            return bundle is null
                ? ControlCloudEntitlementBundlePullResult.Failure(
                    "ControlCloudResponseInvalid",
                    "Control Cloud entitlement response was empty.")
                : ControlCloudEntitlementBundlePullResult.Success(bundle);
        }
        catch (HttpRequestException exception)
        {
            return ControlCloudEntitlementBundlePullResult.Failure(
                "ControlCloudUnavailable",
                exception.Message);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return ControlCloudEntitlementBundlePullResult.Failure(
                "ControlCloudTimeout",
                exception.Message);
        }
    }

    private Uri BuildRequestUri(Guid clientId, string installationId)
    {
        var baseUri = _httpClient.BaseAddress ?? _options.BaseUrl;
        var path = $"/api/v1/local-server/installations/{Uri.EscapeDataString(installationId)}/entitlement-bundle";
        var query = $"clientId={clientId:D}";

        return new Uri(baseUri, $"{path}?{query}");
    }
}
