using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;

public sealed class EfClientPortalMailDeliveryRepository : IClientPortalMailDeliveryRepository
{
    private readonly ControlCloudDbContext _dbContext;

    public EfClientPortalMailDeliveryRepository(ControlCloudDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        ControlCloudClientPortalMailDelivery delivery,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.ClientPortalMailDeliveries
            .AsNoTracking()
            .SingleOrDefaultAsync(
                stored => stored.DeliveryId == delivery.DeliveryId,
                cancellationToken);

        if (existing is not null)
        {
            if (IsSameMessage(existing, delivery))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Mail delivery '{delivery.DeliveryId}' already exists with different content.");
        }

        await _dbContext.ClientPortalMailDeliveries.AddAsync(ToEntity(delivery), cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ControlCloudClientPortalMailDelivery>> ClaimDueAsync(
        DateTimeOffset nowUtc,
        TimeSpan leaseDuration,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        ValidateClaimArguments(leaseDuration, batchSize);
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var entities = _dbContext.Database.IsNpgsql()
            ? await ClaimPostgresRowsAsync(nowUtc, batchSize, cancellationToken)
            : await ClaimPortableRowsAsync(nowUtc, batchSize, cancellationToken);
        var leaseExpiresAtUtc = nowUtc.Add(leaseDuration);
        var claimed = new List<ControlCloudClientPortalMailDelivery>(entities.Length);

        foreach (var entity in entities)
        {
            var delivery = ToDomain(entity);
            delivery.Claim(Guid.NewGuid(), nowUtc, leaseExpiresAtUtc);
            Apply(delivery, entity);
            claimed.Add(delivery);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return claimed;
    }

    public async Task SaveAsync(
        ControlCloudClientPortalMailDelivery delivery,
        Guid expectedLeaseId,
        CancellationToken cancellationToken = default)
    {
        if (expectedLeaseId == Guid.Empty)
        {
            throw new InvalidOperationException("Expected mail delivery lease id is required.");
        }

        var entity = await _dbContext.ClientPortalMailDeliveries
            .SingleOrDefaultAsync(
                stored => stored.DeliveryId == delivery.DeliveryId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"Mail delivery '{delivery.DeliveryId}' was not found.");

        if (entity.LeaseId != expectedLeaseId || delivery.LeaseId != expectedLeaseId)
        {
            throw new InvalidOperationException(
                $"Mail delivery '{delivery.DeliveryId}' no longer holds lease '{expectedLeaseId}'.");
        }

        Apply(delivery, entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task<ControlCloudClientPortalMailDeliveryEntity[]> ClaimPostgresRowsAsync(
        DateTimeOffset nowUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var pending = ControlCloudClientPortalMailDeliveryStatuses.Pending;
        var processing = ControlCloudClientPortalMailDeliveryStatuses.Processing;

        return _dbContext.ClientPortalMailDeliveries
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM cloud.client_portal_mail_deliveries
                WHERE (status = {pending} AND next_attempt_at_utc <= {nowUtc})
                   OR (status = {processing}
                       AND lease_expires_at_utc IS NOT NULL
                       AND lease_expires_at_utc <= {nowUtc})
                ORDER BY next_attempt_at_utc, created_at_utc
                FOR UPDATE SKIP LOCKED
                LIMIT {batchSize}
                """)
            .ToArrayAsync(cancellationToken);
    }

    private Task<ControlCloudClientPortalMailDeliveryEntity[]> ClaimPortableRowsAsync(
        DateTimeOffset nowUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        return _dbContext.ClientPortalMailDeliveries
            .Where(delivery =>
                delivery.Status == ControlCloudClientPortalMailDeliveryStatuses.Pending
                && delivery.NextAttemptAtUtc <= nowUtc
                || delivery.Status == ControlCloudClientPortalMailDeliveryStatuses.Processing
                && delivery.LeaseExpiresAtUtc != null
                && delivery.LeaseExpiresAtUtc <= nowUtc)
            .OrderBy(delivery => delivery.NextAttemptAtUtc)
            .ThenBy(delivery => delivery.CreatedAtUtc)
            .Take(batchSize)
            .ToArrayAsync(cancellationToken);
    }

    private static ControlCloudClientPortalMailDelivery ToDomain(
        ControlCloudClientPortalMailDeliveryEntity entity)
    {
        return new ControlCloudClientPortalMailDelivery
        {
            DeliveryId = entity.DeliveryId,
            ClientId = entity.ClientId,
            RecipientEmail = entity.RecipientEmail,
            RecipientName = entity.RecipientName,
            Subject = entity.Subject,
            TextBody = entity.TextBody,
            Status = entity.Status,
            AttemptCount = entity.AttemptCount,
            NextAttemptAtUtc = entity.NextAttemptAtUtc,
            CreatedAtUtc = entity.CreatedAtUtc,
            LastAttemptedAtUtc = entity.LastAttemptedAtUtc,
            SentAtUtc = entity.SentAtUtc,
            FailedAtUtc = entity.FailedAtUtc,
            LastError = entity.LastError,
            LeaseId = entity.LeaseId,
            LeaseExpiresAtUtc = entity.LeaseExpiresAtUtc
        };
    }

    private static ControlCloudClientPortalMailDeliveryEntity ToEntity(
        ControlCloudClientPortalMailDelivery delivery)
    {
        var entity = new ControlCloudClientPortalMailDeliveryEntity();
        Apply(delivery, entity);
        return entity;
    }

    private static void Apply(
        ControlCloudClientPortalMailDelivery source,
        ControlCloudClientPortalMailDeliveryEntity target)
    {
        target.DeliveryId = source.DeliveryId;
        target.ClientId = source.ClientId;
        target.RecipientEmail = source.RecipientEmail;
        target.RecipientName = source.RecipientName;
        target.Subject = source.Subject;
        target.TextBody = source.TextBody;
        target.Status = source.Status;
        target.AttemptCount = source.AttemptCount;
        target.NextAttemptAtUtc = source.NextAttemptAtUtc;
        target.CreatedAtUtc = source.CreatedAtUtc;
        target.LastAttemptedAtUtc = source.LastAttemptedAtUtc;
        target.SentAtUtc = source.SentAtUtc;
        target.FailedAtUtc = source.FailedAtUtc;
        target.LastError = source.LastError;
        target.LeaseId = source.LeaseId;
        target.LeaseExpiresAtUtc = source.LeaseExpiresAtUtc;
    }

    private static bool IsSameMessage(
        ControlCloudClientPortalMailDeliveryEntity left,
        ControlCloudClientPortalMailDelivery right)
    {
        return left.ClientId == right.ClientId
            && string.Equals(left.RecipientEmail, right.RecipientEmail, StringComparison.Ordinal)
            && string.Equals(left.RecipientName, right.RecipientName, StringComparison.Ordinal)
            && string.Equals(left.Subject, right.Subject, StringComparison.Ordinal)
            && string.Equals(left.TextBody, right.TextBody, StringComparison.Ordinal)
            && left.CreatedAtUtc == right.CreatedAtUtc;
    }

    private static void ValidateClaimArguments(TimeSpan leaseDuration, int batchSize)
    {
        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        }

        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        }
    }
}
