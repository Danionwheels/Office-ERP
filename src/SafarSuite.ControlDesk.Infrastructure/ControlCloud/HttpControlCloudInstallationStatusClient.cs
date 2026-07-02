using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Infrastructure.ControlCloud;

public sealed class HttpControlCloudInstallationStatusClient
    : IControlCloudInstallationStatusClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<ControlCloudStatusOptions> _options;

    public HttpControlCloudInstallationStatusClient(
        HttpClient httpClient,
        IOptions<ControlCloudStatusOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<ControlCloudInstallationStatusClientResult> GetStatusAsync(
        Guid clientId,
        string installationId,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(_options.Value.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            return ControlCloudInstallationStatusClientResult.Failure(
                "ControlCloudStatusNotConfigured",
                "Control Cloud status base URL is not configured.");
        }

        try
        {
            var requestUri = new Uri(
                baseUri,
                $"/api/v1/control-cloud/clients/{clientId:D}/installations/{Uri.EscapeDataString(installationId.Trim())}/status");
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var status = await response.Content
                    .ReadFromJsonAsync<ControlCloudInstallationStatusResponse>(
                        cancellationToken: cancellationToken);

                return status is null
                    ? ControlCloudInstallationStatusClientResult.Failure(
                        "ControlCloudStatusResponseInvalid",
                        "Control Cloud returned an empty installation status response.")
                    : ControlCloudInstallationStatusClientResult.Success(status);
            }

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ControlCloudInstallationStatusClientResult.Failure(
                    "InstallationNotFound",
                    "Control Cloud does not know this installation."),
                HttpStatusCode.Conflict => ControlCloudInstallationStatusClientResult.Failure(
                    "InstallationClientMismatch",
                    "Control Cloud installation belongs to another client."),
                _ => ControlCloudInstallationStatusClientResult.Failure(
                    "ControlCloudStatusUnavailable",
                    $"Control Cloud status returned HTTP {(int)response.StatusCode}.")
            };
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ControlCloudInstallationStatusClientResult.Failure(
                "ControlCloudStatusUnavailable",
                "Timed out while reading Control Cloud installation status.");
        }
        catch (HttpRequestException exception)
        {
            return ControlCloudInstallationStatusClientResult.Failure(
                "ControlCloudStatusUnavailable",
                exception.Message);
        }
    }
}
