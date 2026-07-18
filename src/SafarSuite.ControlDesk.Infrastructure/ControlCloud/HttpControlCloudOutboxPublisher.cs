using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;

namespace SafarSuite.ControlDesk.Infrastructure.ControlCloud;

public sealed class HttpControlCloudOutboxPublisher : ICloudOutboxPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ControlCloudEnvelopeBuilder _envelopeBuilder;
    private readonly IOptions<ControlCloudPublisherOptions> _options;
    private readonly ICloudOutboxPublisherAvailability _availability;

    public HttpControlCloudOutboxPublisher(
        HttpClient httpClient,
        ControlCloudEnvelopeBuilder envelopeBuilder,
        IOptions<ControlCloudPublisherOptions> options,
        ICloudOutboxPublisherAvailability availability)
    {
        _httpClient = httpClient;
        _envelopeBuilder = envelopeBuilder;
        _options = options;
        _availability = availability;
    }

    public async Task<CloudOutboxPublishResult> PublishAsync(
        CloudOutboxMessage message,
        CancellationToken cancellationToken = default)
    {
        if (!_availability.GetSnapshot().CanPublishAutomatically
            || !ControlCloudPublisherEndpointPolicy.TryResolveEndpoint(
                _options.Value,
                _options.Value.RequireHttps,
                out var endpointUri))
        {
            return CloudOutboxPublishResult.Failure(
                "Control Cloud publisher endpoint is not securely configured.",
                shouldRetry: false);
        }

        try
        {
            var envelope = _envelopeBuilder.Build(message);
            using var response = await _httpClient.PostAsJsonAsync(
                endpointUri,
                envelope,
                JsonOptions,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return CloudOutboxPublishResult.Success(
                    ResolveCloudReference(response, envelope.IdempotencyKey),
                    envelope.Signature.Value);
            }

            return CloudOutboxPublishResult.Failure(
                $"Control Cloud returned HTTP {(int)response.StatusCode} ({response.StatusCode}).",
                IsRetryable(response.StatusCode));
        }
        catch (JsonException)
        {
            return CloudOutboxPublishResult.Failure(
                "Control Cloud payload could not be prepared.",
                shouldRetry: false);
        }
        catch (InvalidOperationException)
        {
            return CloudOutboxPublishResult.Failure(
                "Control Cloud publisher is not configured.",
                shouldRetry: false);
        }
        catch (HttpRequestException)
        {
            return CloudOutboxPublishResult.Failure("Control Cloud request failed.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CloudOutboxPublishResult.Failure("Control Cloud request timed out.");
        }
    }

    private static bool IsRetryable(HttpStatusCode statusCode)
    {
        var numericStatusCode = (int)statusCode;

        return statusCode is not (HttpStatusCode.BadRequest
            or HttpStatusCode.RequestEntityTooLarge
            or HttpStatusCode.UnprocessableEntity)
            && numericStatusCode >= 100;
    }

    private static string ResolveCloudReference(HttpResponseMessage response, string fallback)
    {
        return response.Headers.TryGetValues("X-SafarSuite-Cloud-Message-Id", out var values)
            ? values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? fallback
            : fallback;
    }
}
