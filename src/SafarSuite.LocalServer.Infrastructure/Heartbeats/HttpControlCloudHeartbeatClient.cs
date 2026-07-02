using System.Net.Http.Json;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Heartbeats.Ports;
using SafarSuite.LocalServer.Infrastructure.Entitlements;

namespace SafarSuite.LocalServer.Infrastructure.Heartbeats;

public sealed class HttpControlCloudHeartbeatClient : IControlCloudHeartbeatClient
{
    private readonly HttpClient _httpClient;
    private readonly ControlCloudEntitlementPullOptions _options;

    public HttpControlCloudHeartbeatClient(
        HttpClient httpClient,
        ControlCloudEntitlementPullOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<ControlCloudHeartbeatReportResult> ReportHeartbeatAsync(
        string installationId,
        ReportLocalServerHeartbeatRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestUri = BuildRequestUri(installationId);
            using var response = await _httpClient.PostAsJsonAsync(
                requestUri,
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return response.StatusCode == System.Net.HttpStatusCode.NotFound
                    ? ControlCloudHeartbeatReportResult.Failure(
                        "InstallationNotFound",
                        "Control Cloud does not know this installation.")
                    : ControlCloudHeartbeatReportResult.Failure(
                        "ControlCloudHeartbeatFailed",
                        $"Control Cloud heartbeat failed with HTTP {(int)response.StatusCode}.");
            }

            var heartbeat = await response.Content.ReadFromJsonAsync<LocalServerHeartbeatResponse>(
                cancellationToken: cancellationToken);

            return heartbeat is null
                ? ControlCloudHeartbeatReportResult.Failure(
                    "ControlCloudHeartbeatResponseInvalid",
                    "Control Cloud returned an empty heartbeat response.")
                : ControlCloudHeartbeatReportResult.Success(heartbeat);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ControlCloudHeartbeatReportResult.Failure(
                "ControlCloudTimeout",
                "Timed out while reporting heartbeat to Control Cloud.");
        }
        catch (HttpRequestException exception)
        {
            return ControlCloudHeartbeatReportResult.Failure(
                "ControlCloudUnavailable",
                exception.Message);
        }
    }

    private Uri BuildRequestUri(string installationId)
    {
        var baseUrl = _httpClient.BaseAddress ?? _options.BaseUrl;
        var escapedInstallationId = Uri.EscapeDataString(installationId.Trim());

        return new Uri(
            baseUrl,
            $"/api/v1/local-server/installations/{escapedInstallationId}/heartbeat");
    }
}
