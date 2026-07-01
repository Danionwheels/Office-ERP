using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;

public interface ICloudOutboxMessageRepository
{
    Task AddAsync(CloudOutboxMessage message, CancellationToken cancellationToken = default);

    Task<CloudOutboxMessage?> GetByIdAsync(CloudOutboxMessageId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CloudOutboxMessage>> ListAsync(
        CloudOutboxMessageStatus? status = null,
        string? messageType = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CloudOutboxMessage>> ListReadyForPublishingAsync(
        int batchSize,
        DateTimeOffset readyAtUtc,
        int maximumAttemptCount,
        CancellationToken cancellationToken = default);
}
