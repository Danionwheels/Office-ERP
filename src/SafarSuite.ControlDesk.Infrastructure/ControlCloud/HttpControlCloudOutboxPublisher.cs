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

    public HttpControlCloudOutboxPublisher(
        HttpClient httpClient,
        ControlCloudEnvelopeBuilder envelopeBuilder,
        IOptions<ControlCloudPublisherOptions> options)
    {
        _httpClient = httpClient;
        _envelopeBuilder = envelopeBuilder;
        _options = options;
    }

    public async Task<CloudOutboxPublishResult> PublishAsync(
        CloudOutboxMessage message,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(_options.Value.EndpointUrl, UriKind.Absolute, out var endpointUri))
        {
            return CloudOutboxPublishResult.Failure(
                "Control Cloud publisher endpoint is not configured.",
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

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var detail = string.IsNullOrWhiteSpace(body)
                ? response.ReasonPhrase
                : body;

            return CloudOutboxPublishResult.Failure(
                $"Control Cloud returned {(int)response.StatusCode} {response.StatusCode}: {detail}",
                IsRetryable(response.StatusCode));
        }
        catch (JsonException exception)
        {
            return CloudOutboxPublishResult.Failure(
                $"Control Cloud payload could not be prepared: {exception.Message}",
                shouldRetry: false);
        }
        catch (InvalidOperationException exception)
        {
            return CloudOutboxPublishResult.Failure(
                $"Control Cloud publisher is not configured: {exception.Message}",
                shouldRetry: false);
        }
        catch (HttpRequestException exception)
        {
            return CloudOutboxPublishResult.Failure(
                $"Control Cloud request failed: {exception.Message}");
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return CloudOutboxPublishResult.Failure(
                $"Control Cloud request timed out: {exception.Message}");
        }
    }

    private static bool IsRetryable(HttpStatusCode statusCode)
    {
        var numericStatusCode = (int)statusCode;

        return statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
            || numericStatusCode >= 500;
    }

    private static string ResolveCloudReference(HttpResponseMessage response, string fallback)
    {
        return response.Headers.TryGetValues("X-SafarSuite-Cloud-Message-Id", out var values)
            ? values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? fallback
            : fallback;
    }
}
