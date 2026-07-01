using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
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

    public async Task<IReadOnlyCollection<CloudOutboxMessage>> ListAsync(
        CloudOutboxMessageStatus? status = null,
        string? messageType = null,
        CancellationToken cancellationToken = default)
    {
        var messages = _dbContext.CloudOutboxMessages
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue)
        {
            messages = messages.Where(message => message.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(messageType))
        {
            var cleanMessageType = messageType.Trim();
            messages = messages.Where(message => message.MessageType == cleanMessageType);
        }

        return await messages
            .OrderByDescending(message => message.OccurredAtUtc)
            .ThenBy(message => message.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CloudOutboxMessage>> ListReadyForPublishingAsync(
        int batchSize,
        DateTimeOffset readyAtUtc,
        int maximumAttemptCount,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CloudOutboxMessages
            .Where(message =>
                message.Status == CloudOutboxMessageStatus.Pending
                || (message.Status == CloudOutboxMessageStatus.Failed
                    && message.AttemptCount < maximumAttemptCount
                    && message.NextAttemptAtUtc != null
                    && message.NextAttemptAtUtc <= readyAtUtc))
            .Where(message => message.AttemptCount < maximumAttemptCount)
            .OrderBy(message => message.OccurredAtUtc)
            .ThenBy(message => message.Id)
            .Take(batchSize)
            .ToArrayAsync(cancellationToken);
    }
}
