using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryCloudOutboxMessageRepository : ICloudOutboxMessageRepository
{
    private readonly ConcurrentDictionary<Guid, CloudOutboxMessage> _messagesById = new();

    public Task AddAsync(CloudOutboxMessage message, CancellationToken cancellationToken = default)
    {
        _messagesById.TryAdd(message.Id.Value, message);

        return Task.CompletedTask;
    }

    public Task<CloudOutboxMessage?> GetByIdAsync(
        CloudOutboxMessageId id,
        CancellationToken cancellationToken = default)
    {
        _messagesById.TryGetValue(id.Value, out var message);

        return Task.FromResult(message);
    }

    public Task<IReadOnlyCollection<CloudOutboxMessage>> ListPageAsync(
        CloudOutboxMessageStatus? status,
        string? messageType,
        ClientId? clientId,
        DateTimeOffset? beforeOccurredAtUtc,
        CloudOutboxMessageId? beforeMessageId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var messages = ApplyFilters(_messagesById.Values, status, messageType, clientId);

        if (beforeOccurredAtUtc.HasValue && beforeMessageId.HasValue)
        {
            var occurredAtUtc = beforeOccurredAtUtc.Value;
            var messageId = beforeMessageId.Value.Value;
            messages = messages.Where(message =>
                message.OccurredAtUtc < occurredAtUtc
                || (message.OccurredAtUtc == occurredAtUtc
                    && message.Id.Value.CompareTo(messageId) < 0));
        }

        var page = messages
            .OrderByDescending(message => message.OccurredAtUtc)
            .ThenByDescending(message => message.Id.Value)
            .Take(Math.Max(take, 0))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CloudOutboxMessage>>(page);
    }

    public Task<CloudOutboxMessageRegisterSummary> SummarizeAsync(
        CloudOutboxMessageStatus? status,
        string? messageType,
        ClientId? clientId,
        DateTimeOffset readyAtUtc,
        int maximumAttemptCount,
        CancellationToken cancellationToken = default)
    {
        var messages = ApplyFilters(_messagesById.Values, status, messageType, clientId).ToArray();
        var summary = new CloudOutboxMessageRegisterSummary(
            messages.LongLength,
            messages.LongCount(message => message.Status == CloudOutboxMessageStatus.Pending),
            messages.LongCount(message => message.Status == CloudOutboxMessageStatus.Failed),
            messages.LongCount(message => message.Status == CloudOutboxMessageStatus.Sent),
            messages.LongCount(message => message.IsReadyForPublishing(readyAtUtc, maximumAttemptCount)),
            messages.Sum(message => (long)message.AttemptCount));

        return Task.FromResult(summary);
    }

    public Task<IReadOnlyCollection<CloudOutboxMessage>> ListReadyForPublishingAsync(
        int batchSize,
        DateTimeOffset readyAtUtc,
        int maximumAttemptCount,
        CancellationToken cancellationToken = default)
    {
        var messages = _messagesById.Values
            .Where(message => message.IsReadyForPublishing(readyAtUtc, maximumAttemptCount))
            .OrderBy(message => message.OccurredAtUtc)
            .ThenBy(message => message.Id.Value)
            .Take(batchSize)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CloudOutboxMessage>>(messages);
    }

    private static IEnumerable<CloudOutboxMessage> ApplyFilters(
        IEnumerable<CloudOutboxMessage> messages,
        CloudOutboxMessageStatus? status,
        string? messageType,
        ClientId? clientId)
    {
        if (status.HasValue)
        {
            messages = messages.Where(message => message.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(messageType))
        {
            messages = messages.Where(message =>
                string.Equals(message.MessageType, messageType.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (clientId.HasValue)
        {
            messages = messages.Where(message => message.ClientId == clientId.Value);
        }

        return messages;
    }
}
