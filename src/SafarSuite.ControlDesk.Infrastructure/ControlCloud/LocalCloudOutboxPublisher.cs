using System.Text.Json;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;

namespace SafarSuite.ControlDesk.Infrastructure.ControlCloud;

public sealed class LocalCloudOutboxPublisher : ICloudOutboxPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ControlCloudEnvelopeBuilder _envelopeBuilder;

    public LocalCloudOutboxPublisher(ControlCloudEnvelopeBuilder envelopeBuilder)
    {
        _envelopeBuilder = envelopeBuilder;
    }

    public Task<CloudOutboxPublishResult> PublishAsync(
        CloudOutboxMessage message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var envelope = _envelopeBuilder.Build(message);
            var envelopeJson = JsonSerializer.Serialize(envelope, JsonOptions);
            using var _ = JsonDocument.Parse(envelopeJson);

            return Task.FromResult(CloudOutboxPublishResult.Success(
                envelope.IdempotencyKey,
                envelope.Signature.Value));
        }
        catch (JsonException exception)
        {
            return Task.FromResult(CloudOutboxPublishResult.Failure(
                $"Local publisher could not parse payload JSON: {exception.Message}"));
        }
        catch (InvalidOperationException exception)
        {
            return Task.FromResult(CloudOutboxPublishResult.Failure(
                $"Local publisher is not configured: {exception.Message}",
                shouldRetry: false));
        }
    }
}
