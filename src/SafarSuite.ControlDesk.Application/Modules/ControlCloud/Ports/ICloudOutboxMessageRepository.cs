using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;

public interface ICloudOutboxMessageRepository
{
    Task AddAsync(CloudOutboxMessage message, CancellationToken cancellationToken = default);

    Task<CloudOutboxMessage?> GetByIdAsync(CloudOutboxMessageId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CloudOutboxMessage>> ListPageAsync(
        CloudOutboxMessageStatus? status,
        string? messageType,
        ClientId? clientId,
        DateTimeOffset? beforeOccurredAtUtc,
        CloudOutboxMessageId? beforeMessageId,
        int take,
        CancellationToken cancellationToken = default);

    Task<CloudOutboxMessageRegisterSummary> SummarizeAsync(
        CloudOutboxMessageStatus? status,
        string? messageType,
        ClientId? clientId,
        DateTimeOffset readyAtUtc,
        int maximumAttemptCount,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CloudOutboxMessage>> ListReadyForPublishingAsync(
        int batchSize,
        DateTimeOffset readyAtUtc,
        int maximumAttemptCount,
        CancellationToken cancellationToken = default);
}

public sealed record CloudOutboxMessageRegisterSummary(
    long TotalCount,
    long PendingCount,
    long FailedCount,
    long SentCount,
    long ReadyForPublishingCount,
    long TotalAttemptCount)
{
    public static CloudOutboxMessageRegisterSummary Empty { get; } = new(0, 0, 0, 0, 0, 0);
}
