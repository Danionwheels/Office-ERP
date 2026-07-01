using System.Text.Json;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;

namespace SafarSuite.ControlDesk.Infrastructure.ControlCloud;

public sealed class LocalCloudOutboxPublisher : ICloudOutboxPublisher
{
    public Task<CloudOutboxPublishResult> PublishAsync(
        CloudOutboxMessage message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var _ = JsonDocument.Parse(message.PayloadJson);

            return Task.FromResult(CloudOutboxPublishResult.Success());
        }
        catch (JsonException exception)
        {
            return Task.FromResult(CloudOutboxPublishResult.Failure(
                $"Local publisher could not parse payload JSON: {exception.Message}"));
        }
    }
}
