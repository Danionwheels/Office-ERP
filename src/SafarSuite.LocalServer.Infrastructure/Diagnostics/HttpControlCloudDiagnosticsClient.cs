using System.Net;
using System.Net.Http.Json;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Diagnostics.Ports;
using SafarSuite.LocalServer.Infrastructure.Entitlements;

namespace SafarSuite.LocalServer.Infrastructure.Diagnostics;

public sealed class HttpControlCloudDiagnosticsClient : IControlCloudDiagnosticsClient
{
    private readonly HttpClient _httpClient;
    private readonly ControlCloudEntitlementPullOptions _options;

    public HttpControlCloudDiagnosticsClient(
        HttpClient httpClient,
        ControlCloudEntitlementPullOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<ControlCloudDiagnosticsUploadResult> UploadAsync(
        string installationId,
        UploadLocalServerDiagnosticsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                BuildRequestUri(installationId),
                request,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return ControlCloudDiagnosticsUploadResult.Failure(
                    "InstallationNotFound",
                    "Control Cloud does not know this installation.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return ControlCloudDiagnosticsUploadResult.Failure(
                    "ControlCloudDiagnosticsUploadFailed",
                    $"Control Cloud diagnostics upload failed with HTTP {(int)response.StatusCode}.");
            }

            var upload = await response.Content.ReadFromJsonAsync<LocalServerDiagnosticsUploadResponse>(
                cancellationToken);

            return upload is null
                ? ControlCloudDiagnosticsUploadResult.Failure(
                    "ControlCloudDiagnosticsResponseInvalid",
                    "Control Cloud returned an empty diagnostics upload response.")
                : ControlCloudDiagnosticsUploadResult.Success(upload);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return ControlCloudDiagnosticsUploadResult.Failure(
                "ControlCloudTimeout",
                exception.Message);
        }
        catch (HttpRequestException exception)
        {
            return ControlCloudDiagnosticsUploadResult.Failure(
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
            $"/api/v1/local-server/installations/{escapedInstallationId}/diagnostics");
    }
}
