using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
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

    public Task<IReadOnlyCollection<CloudOutboxMessage>> ListAsync(
        CloudOutboxMessageStatus? status = null,
        string? messageType = null,
        CancellationToken cancellationToken = default)
    {
        var messages = _messagesById.Values.AsEnumerable();

        if (status.HasValue)
        {
            messages = messages.Where(message => message.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(messageType))
        {
            messages = messages.Where(message =>
                string.Equals(message.MessageType, messageType.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        var orderedMessages = messages
            .OrderByDescending(message => message.OccurredAtUtc)
            .ThenBy(message => message.Id.Value)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CloudOutboxMessage>>(orderedMessages);
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
}
