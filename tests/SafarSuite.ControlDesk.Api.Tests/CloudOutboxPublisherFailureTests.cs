using System.Net;
using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;
using SafarSuite.ControlDesk.Infrastructure.ControlCloud;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class CloudOutboxPublisherFailureTests
{
    private static readonly DateTimeOffset OccurredAtUtc =
        new(2026, 7, 18, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Local_publisher_treats_malformed_payload_as_nonretryable()
    {
        var options = CreateOptions(endpointUrl: null);
        var publisher = new LocalCloudOutboxPublisher(
            new ControlCloudEnvelopeBuilder(options, new FixedClock()));
        var message = CreateMessage("not-json");

        var result = await publisher.PublishAsync(message);

        Assert.False(result.IsSuccess);
        Assert.False(result.ShouldRetry);
        Assert.Contains("could not parse payload JSON", result.FailureReason);
    }

    [Fact]
    public async Task Http_publisher_uses_stable_sanitized_failure_without_response_body()
    {
        const string secretResponseBody = "sensitive upstream detail must not be retained";
        var options = CreateOptions("https://cloud.example.test/api/v1/control-desk/messages");
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent(secretResponseBody)
            }));
        var publisher = new HttpControlCloudOutboxPublisher(
            httpClient,
            new ControlCloudEnvelopeBuilder(options, new FixedClock()),
            options,
            AvailablePublisher());

        var result = await publisher.PublishAsync(CreateMessage("{\"value\":1}"));

        Assert.False(result.IsSuccess);
        Assert.True(result.ShouldRetry);
        Assert.Equal("Control Cloud returned HTTP 503 (ServiceUnavailable).", result.FailureReason);
        Assert.DoesNotContain(secretResponseBody, result.FailureReason);
    }

    [Fact]
    public async Task Http_publisher_retries_authentication_failure_after_credentials_change()
    {
        var options = CreateOptions("https://cloud.example.test/api/v1/control-desk/messages");
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        var publisher = new HttpControlCloudOutboxPublisher(
            httpClient,
            new ControlCloudEnvelopeBuilder(options, new FixedClock()),
            options,
            AvailablePublisher());

        var result = await publisher.PublishAsync(CreateMessage("{\"value\":1}"));

        Assert.False(result.IsSuccess);
        Assert.True(result.ShouldRetry);
        Assert.Equal("Control Cloud returned HTTP 401 (Unauthorized).", result.FailureReason);
    }

    [Fact]
    public void Configured_policy_preserves_zero_as_unlimited()
    {
        var options = CreateOptions(endpointUrl: null);
        options.Value.MaximumAttemptCount = 0;

        var policy = new ConfiguredCloudOutboxPublishPolicy(options);

        Assert.Equal(0, policy.MaximumAttemptCount);
    }

    private static IOptions<ControlCloudPublisherOptions> CreateOptions(string? endpointUrl) =>
        Options.Create(new ControlCloudPublisherOptions
        {
            Mode = endpointUrl is null ? "Local" : "Http",
            SourceSystem = "SafarSuite.ControlDesk",
            Environment = "Test",
            SigningKeyId = "test-key",
            SigningSecret = "test-signing-secret-at-least-32-characters",
            EndpointUrl = endpointUrl,
            RequireHttps = endpointUrl is not null,
            MaximumAttemptCount = 5,
            RetryDelaySeconds = 60
        });

    private static ICloudOutboxPublisherAvailability AvailablePublisher() =>
        new StaticPublisherAvailability();

    private static CloudOutboxMessage CreateMessage(string payloadJson) =>
        CloudOutboxMessage.Create(
            CloudOutboxMessageId.Create(Guid.NewGuid()),
            ClientId.Create(Guid.NewGuid()),
            "TestMessage",
            "TestSubject",
            Guid.NewGuid().ToString("D"),
            payloadJson,
            OccurredAtUtc);

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => OccurredAtUtc;

        public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
    }

    private sealed class StaticPublisherAvailability : ICloudOutboxPublisherAvailability
    {
        public CloudOutboxPublisherAvailabilitySnapshot GetSnapshot() =>
            new(true, true, "Ready");
    }

    private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }
}
