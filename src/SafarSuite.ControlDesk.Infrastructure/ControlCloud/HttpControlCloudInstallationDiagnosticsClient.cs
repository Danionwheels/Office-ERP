using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Infrastructure.ControlCloud;

public sealed class HttpControlCloudInstallationDiagnosticsClient
    : IControlCloudInstallationDiagnosticsClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<ControlCloudStatusOptions> _options;

    public HttpControlCloudInstallationDiagnosticsClient(
        HttpClient httpClient,
        IOptions<ControlCloudStatusOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<ControlCloudInstallationDiagnosticsClientResult> GetLatestAsync(
        Guid clientId,
        string installationId,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(_options.Value.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            return ControlCloudInstallationDiagnosticsClientResult.Failure(
                "ControlCloudDiagnosticsNotConfigured",
                "Control Cloud diagnostics base URL is not configured.");
        }

        try
        {
            var requestUri = new Uri(
                baseUri,
                $"/api/v1/control-cloud/clients/{clientId:D}/installations/{Uri.EscapeDataString(installationId.Trim())}/diagnostics/latest");
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var report = await response.Content
                    .ReadFromJsonAsync<LocalServerDiagnosticReportResponse>(
                        cancellationToken: cancellationToken);

                return report is null
                    ? ControlCloudInstallationDiagnosticsClientResult.Failure(
                        "ControlCloudDiagnosticsResponseInvalid",
                        "Control Cloud returned an empty diagnostics response.")
                    : ControlCloudInstallationDiagnosticsClientResult.Success(report);
            }

            var error = await ReadErrorAsync(response, cancellationToken);

            return ControlCloudInstallationDiagnosticsClientResult.Failure(
                error.Code ?? ToDefaultFailureCode(response.StatusCode),
                error.Detail);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ControlCloudInstallationDiagnosticsClientResult.Failure(
                "ControlCloudDiagnosticsUnavailable",
                "Timed out while reading Control Cloud diagnostics.");
        }
        catch (HttpRequestException exception)
        {
            return ControlCloudInstallationDiagnosticsClientResult.Failure(
                "ControlCloudDiagnosticsUnavailable",
                exception.Message);
        }
    }

    private static string ToDefaultFailureCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.NotFound => "DiagnosticsNotFound",
            HttpStatusCode.Conflict => "InstallationClientMismatch",
            _ => "ControlCloudDiagnosticsUnavailable"
        };
    }

    private static async Task<CloudError> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<CloudError>(
                cancellationToken: cancellationToken);

            if (error is not null)
            {
                return error with
                {
                    Detail = string.IsNullOrWhiteSpace(error.Detail)
                        ? $"Control Cloud returned HTTP {(int)response.StatusCode}."
                        : error.Detail
                };
            }
        }
        catch (InvalidOperationException)
        {
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var detail = string.IsNullOrWhiteSpace(body)
            ? $"Control Cloud returned HTTP {(int)response.StatusCode}."
            : body;

        return new CloudError(null, detail);
    }

    private sealed record CloudError(string? Code, string Detail);
}
