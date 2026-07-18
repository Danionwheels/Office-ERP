using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.PublishPendingCloudOutboxMessages;

namespace SafarSuite.ControlDesk.Api.Modules.ControlCloud;

public sealed class ControlCloudOutboxPublishCoordinator(
    IServiceScopeFactory scopeFactory,
    ICloudOutboxPublisherAvailability publisherAvailability) : IDisposable
{
    private readonly SemaphoreSlim _publicationGate = new(1, 1);

    public bool IsPublisherConfigured => publisherAvailability.GetSnapshot().CanPublish;

    public bool IsAutomaticPublisherConfigured =>
        publisherAvailability.GetSnapshot().CanPublishAutomatically;

    public async Task<Result<PublishPendingCloudOutboxMessagesResult>> PublishAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (!IsPublisherConfigured)
        {
            return Result<PublishPendingCloudOutboxMessagesResult>.Failure(
                ApplicationError.ServiceUnavailable(
                    "Control Cloud publisher is not securely configured.",
                    "ControlCloudPublisher"));
        }

        await _publicationGate.WaitAsync(cancellationToken);

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider
                .GetRequiredService<PublishPendingCloudOutboxMessagesHandler>();

            return await handler.HandleAsync(
                new PublishPendingCloudOutboxMessagesCommand(batchSize),
                cancellationToken);
        }
        finally
        {
            _publicationGate.Release();
        }
    }

    public void Dispose()
    {
        _publicationGate.Dispose();
    }
}
