using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Infrastructure.ControlCloud;

public sealed class HttpControlCloudAuditClient : IControlCloudAuditClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<ControlCloudStatusOptions> _options;

    public HttpControlCloudAuditClient(
        HttpClient httpClient,
        IOptions<ControlCloudStatusOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<ControlCloudAuditClientResult> ListEventsAsync(
        Guid clientId,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(_options.Value.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            return ControlCloudAuditClientResult.Failure(
                "ControlCloudAuditNotConfigured",
                "Control Cloud audit base URL is not configured.");
        }

        try
        {
            var requestUri = new Uri(
                baseUri,
                $"/api/v1/control-cloud/audit-events?clientId={clientId:D}&take={Math.Clamp(take, 1, 500)}");
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var auditEvents = await response.Content
                    .ReadFromJsonAsync<ControlCloudAuditEventsResponse>(
                        cancellationToken: cancellationToken);

                return auditEvents is null
                    ? ControlCloudAuditClientResult.Failure(
                        "ControlCloudAuditResponseInvalid",
                        "Control Cloud returned an empty audit response.")
                    : ControlCloudAuditClientResult.Success(auditEvents.Events);
            }

            return ControlCloudAuditClientResult.Failure(
                "ControlCloudAuditUnavailable",
                $"Control Cloud audit returned HTTP {(int)response.StatusCode}.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ControlCloudAuditClientResult.Failure(
                "ControlCloudAuditUnavailable",
                "Timed out while reading Control Cloud audit events.");
        }
        catch (HttpRequestException exception)
        {
            return ControlCloudAuditClientResult.Failure(
                "ControlCloudAuditUnavailable",
                exception.Message);
        }
    }
}
