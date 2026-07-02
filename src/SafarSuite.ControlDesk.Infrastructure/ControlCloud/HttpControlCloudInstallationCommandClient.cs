using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Infrastructure.ControlCloud;

public sealed class HttpControlCloudInstallationCommandClient
    : IControlCloudInstallationCommandClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<ControlCloudStatusOptions> _options;

    public HttpControlCloudInstallationCommandClient(
        HttpClient httpClient,
        IOptions<ControlCloudStatusOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<ControlCloudInstallationCommandClientResult> QueueCommandAsync(
        Guid clientId,
        string installationId,
        QueueInstallationCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(_options.Value.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            return ControlCloudInstallationCommandClientResult.Failure(
                "ControlCloudCommandNotConfigured",
                "Control Cloud command base URL is not configured.");
        }

        try
        {
            var requestUri = new Uri(
                baseUri,
                $"/api/v1/control-cloud/clients/{clientId:D}/installations/{Uri.EscapeDataString(installationId.Trim())}/commands");
            using var response = await _httpClient.PostAsJsonAsync(
                requestUri,
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var queuedCommand = await response.Content
                    .ReadFromJsonAsync<InstallationCommandResponse>(
                        cancellationToken: cancellationToken);

                return queuedCommand is null
                    ? ControlCloudInstallationCommandClientResult.Failure(
                        "ControlCloudCommandResponseInvalid",
                        "Control Cloud returned an empty command response.")
                    : ControlCloudInstallationCommandClientResult.Success(queuedCommand);
            }

            var error = await ReadErrorAsync(response, cancellationToken);

            return ControlCloudInstallationCommandClientResult.Failure(
                error.Code ?? ToDefaultFailureCode(response.StatusCode),
                error.Detail);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ControlCloudInstallationCommandClientResult.Failure(
                "ControlCloudCommandUnavailable",
                "Timed out while queueing the Control Cloud command.");
        }
        catch (HttpRequestException exception)
        {
            return ControlCloudInstallationCommandClientResult.Failure(
                "ControlCloudCommandUnavailable",
                exception.Message);
        }
    }

    private static string ToDefaultFailureCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.NotFound => "InstallationNotFound",
            HttpStatusCode.Conflict => "InstallationClientMismatch",
            HttpStatusCode.BadRequest => "CommandInvalid",
            _ => "ControlCloudCommandUnavailable"
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
