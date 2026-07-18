using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfCloudOutboxMessageRepository : ICloudOutboxMessageRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfCloudOutboxMessageRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(CloudOutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _dbContext.CloudOutboxMessages.AddAsync(message, cancellationToken);
    }

    public async Task<CloudOutboxMessage?> GetByIdAsync(
        CloudOutboxMessageId id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CloudOutboxMessages
            .SingleOrDefaultAsync(message => message.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CloudOutboxMessage>> ListPageAsync(
        CloudOutboxMessageStatus? status,
        string? messageType,
        ClientId? clientId,
        DateTimeOffset? beforeOccurredAtUtc,
        CloudOutboxMessageId? beforeMessageId,
        int take,
        CancellationToken cancellationToken = default)
    {
        IQueryable<CloudOutboxMessage> messages;

        if (beforeOccurredAtUtc.HasValue && beforeMessageId.HasValue)
        {
            var occurredAtUtc = beforeOccurredAtUtc.Value;
            var messageId = beforeMessageId.Value.Value;
            messages = _dbContext.CloudOutboxMessages
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM control.cloud_outbox_messages
                    WHERE occurred_at_utc < {occurredAtUtc}
                       OR (occurred_at_utc = {occurredAtUtc}
                           AND cloud_outbox_message_id < {messageId})
                    """)
                .AsNoTracking();
        }
        else
        {
            messages = _dbContext.CloudOutboxMessages.AsNoTracking();
        }

        messages = ApplyFilters(messages, status, messageType, clientId);

        return await messages
            .OrderByDescending(message => message.OccurredAtUtc)
            .ThenByDescending(message => message.Id)
            .Take(Math.Max(take, 0))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<CloudOutboxMessageRegisterSummary> SummarizeAsync(
        CloudOutboxMessageStatus? status,
        string? messageType,
        ClientId? clientId,
        DateTimeOffset readyAtUtc,
        int maximumAttemptCount,
        CancellationToken cancellationToken = default)
    {
        EnsureValidMaximumAttemptCount(maximumAttemptCount);

        var messages = ApplyFilters(
            _dbContext.CloudOutboxMessages.AsNoTracking(),
            status,
            messageType,
            clientId);
        var summary = await messages
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalCount = group.LongCount(),
                PendingCount = group.Sum(message =>
                    message.Status == CloudOutboxMessageStatus.Pending ? 1L : 0L),
                FailedCount = group.Sum(message =>
                    message.Status == CloudOutboxMessageStatus.Failed ? 1L : 0L),
                SentCount = group.Sum(message =>
                    message.Status == CloudOutboxMessageStatus.Sent ? 1L : 0L),
                ReadyForPublishingCount = group.Sum(message =>
                    ((message.Status == CloudOutboxMessageStatus.Pending
                        && (maximumAttemptCount == 0
                            || message.AttemptCount < maximumAttemptCount))
                     || (message.Status == CloudOutboxMessageStatus.Failed
                         && (maximumAttemptCount == 0
                             || message.AttemptCount < maximumAttemptCount)
                         && message.NextAttemptAtUtc != null
                         && message.NextAttemptAtUtc <= readyAtUtc))
                        ? 1L
                        : 0L),
                TotalAttemptCount = group.Sum(message => (long)message.AttemptCount)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return summary is null
            ? CloudOutboxMessageRegisterSummary.Empty
            : new CloudOutboxMessageRegisterSummary(
                summary.TotalCount,
                summary.PendingCount,
                summary.FailedCount,
                summary.SentCount,
                summary.ReadyForPublishingCount,
                summary.TotalAttemptCount);
    }

    public async Task<IReadOnlyCollection<CloudOutboxMessage>> ListReadyForPublishingAsync(
        int batchSize,
        DateTimeOffset readyAtUtc,
        int maximumAttemptCount,
        CancellationToken cancellationToken = default)
    {
        EnsureValidMaximumAttemptCount(maximumAttemptCount);

        return await _dbContext.CloudOutboxMessages
            .Where(message =>
                message.Status == CloudOutboxMessageStatus.Pending
                || (message.Status == CloudOutboxMessageStatus.Failed
                    && message.NextAttemptAtUtc != null
                    && message.NextAttemptAtUtc <= readyAtUtc))
            .Where(message => maximumAttemptCount == 0
                || message.AttemptCount < maximumAttemptCount)
            .OrderBy(message => message.OccurredAtUtc)
            .ThenBy(message => message.Id)
            .Take(batchSize)
            .ToArrayAsync(cancellationToken);
    }

    private static void EnsureValidMaximumAttemptCount(int maximumAttemptCount)
    {
        if (maximumAttemptCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAttemptCount));
        }
    }

    private static IQueryable<CloudOutboxMessage> ApplyFilters(
        IQueryable<CloudOutboxMessage> messages,
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
            var cleanMessageType = messageType.Trim();
            messages = messages.Where(message => message.MessageType == cleanMessageType);
        }

        if (clientId.HasValue)
        {
            var ownedClientId = clientId.Value;
            messages = messages.Where(message => message.ClientId == ownedClientId);
        }

        return messages;
    }
}
