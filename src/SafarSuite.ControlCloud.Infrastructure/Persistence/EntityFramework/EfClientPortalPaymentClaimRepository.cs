using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;

public sealed class EfClientPortalPaymentClaimRepository : IClientPortalPaymentClaimRepository
{
    private readonly ControlCloudDbContext _dbContext;

    public EfClientPortalPaymentClaimRepository(ControlCloudDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<ControlCloudClientPortalPaymentClaim?> GetByIdAsync(
        Guid claimId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ClientPortalPaymentClaims
            .AsNoTracking()
            .SingleOrDefaultAsync(claim => claim.ClaimId == claimId, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<ControlCloudClientPortalPaymentClaim?> GetByClientAndReferenceAsync(
        Guid clientId,
        string normalizedTransferReferenceNumber,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ClientPortalPaymentClaims
            .AsNoTracking()
            .SingleOrDefaultAsync(
                claim => claim.ClientId == clientId
                    && claim.NormalizedTransferReferenceNumber == normalizedTransferReferenceNumber,
                cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyCollection<ControlCloudClientPortalPaymentClaim>> ListAsync(
        Guid? clientId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ClientPortalPaymentClaims.AsNoTracking();

        if (clientId is not null)
        {
            query = query.Where(claim => claim.ClientId == clientId.Value);
        }

        var entities = await query
            .OrderByDescending(claim => claim.SubmittedAtUtc)
            .ThenByDescending(claim => claim.ClaimId)
            .ToArrayAsync(cancellationToken);

        return entities.Select(ToDomain).ToArray();
    }

    public Task AddAsync(
        ControlCloudClientPortalPaymentClaim claim,
        CancellationToken cancellationToken = default) =>
        _dbContext.ClientPortalPaymentClaims.AddAsync(ToEntity(claim), cancellationToken).AsTask();

    public async Task SaveAsync(
        ControlCloudClientPortalPaymentClaim claim,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ClientPortalPaymentClaims
            .SingleAsync(stored => stored.ClaimId == claim.ClaimId, cancellationToken);
        Apply(claim, entity);
        _dbContext.Entry(entity).Property(value => value.ConcurrencyToken).OriginalValue =
            claim.OriginalConcurrencyToken;
    }

    private static ControlCloudClientPortalPaymentClaim ToDomain(
        ControlCloudClientPortalPaymentClaimEntity entity) => new()
    {
        ClaimId = entity.ClaimId,
        ClientId = entity.ClientId,
        SubmittedByUserId = entity.SubmittedByUserId,
        InvoiceId = entity.InvoiceId,
        InvoiceNumber = entity.InvoiceNumber,
        Amount = entity.Amount,
        CurrencyCode = entity.CurrencyCode,
        TransferReferenceNumber = entity.TransferReferenceNumber,
        NormalizedTransferReferenceNumber = entity.NormalizedTransferReferenceNumber,
        ProofAttachmentId = entity.ProofAttachmentId,
        Status = Enum.Parse<ControlCloudClientPortalPaymentClaimStatus>(entity.Status),
        SubmittedAtUtc = entity.SubmittedAtUtc,
        ReviewedAtUtc = entity.ReviewedAtUtc,
        VerifiedPaymentId = entity.VerifiedPaymentId,
        RejectionReason = entity.RejectionReason,
        ConcurrencyToken = entity.ConcurrencyToken,
        OriginalConcurrencyToken = entity.ConcurrencyToken
    };

    private static ControlCloudClientPortalPaymentClaimEntity ToEntity(
        ControlCloudClientPortalPaymentClaim claim)
    {
        var entity = new ControlCloudClientPortalPaymentClaimEntity();
        Apply(claim, entity);
        return entity;
    }

    private static void Apply(
        ControlCloudClientPortalPaymentClaim source,
        ControlCloudClientPortalPaymentClaimEntity target)
    {
        target.ClaimId = source.ClaimId;
        target.ClientId = source.ClientId;
        target.SubmittedByUserId = source.SubmittedByUserId;
        target.InvoiceId = source.InvoiceId;
        target.InvoiceNumber = source.InvoiceNumber;
        target.Amount = source.Amount;
        target.CurrencyCode = source.CurrencyCode;
        target.TransferReferenceNumber = source.TransferReferenceNumber;
        target.NormalizedTransferReferenceNumber = source.NormalizedTransferReferenceNumber;
        target.ProofAttachmentId = source.ProofAttachmentId;
        target.Status = source.Status.ToString();
        target.SubmittedAtUtc = source.SubmittedAtUtc;
        target.ReviewedAtUtc = source.ReviewedAtUtc;
        target.VerifiedPaymentId = source.VerifiedPaymentId;
        target.RejectionReason = source.RejectionReason;
        target.ConcurrencyToken = source.ConcurrencyToken;
    }
}
