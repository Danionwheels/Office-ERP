using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlCloud.Application.Modules.InboundControlDesk.Ports;
using SafarSuite.ControlCloud.Domain.Modules.InboundControlDesk;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;

public sealed class EfControlDeskEnvelopeReceiptRepository : IControlDeskEnvelopeReceiptRepository
{
    private readonly ControlCloudDbContext _dbContext;

    public EfControlDeskEnvelopeReceiptRepository(ControlCloudDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ControlDeskEnvelopeReceipt?> GetAcceptedByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var cleanIdempotencyKey = idempotencyKey.Trim();

        var receipt = await _dbContext.ControlDeskEnvelopeReceipts
            .AsNoTracking()
            .Where(receipt =>
                receipt.Status == ControlDeskEnvelopeReceiptStatus.Accepted.ToString()
                && receipt.IdempotencyKey == cleanIdempotencyKey)
            .OrderBy(receipt => receipt.ReceivedAtUtc)
            .ThenBy(receipt => receipt.ReceiptId)
            .FirstOrDefaultAsync(cancellationToken);

        return receipt is null ? null : ToAcceptedReceipt(receipt);
    }

    public async Task AddAsync(
        ControlDeskEnvelopeReceipt receipt,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.ControlDeskEnvelopeReceipts.AddAsync(
            FromReceipt(receipt),
            cancellationToken);
    }

    private static ControlDeskEnvelopeReceipt ToAcceptedReceipt(ControlDeskEnvelopeReceiptEntity receipt)
    {
        return ControlDeskEnvelopeReceipt.Accepted(
            receipt.ReceiptId,
            receipt.MessageId,
            receipt.MessageType,
            receipt.SubjectType,
            receipt.SubjectId,
            receipt.SourceSystem,
            receipt.SourceEnvironment,
            receipt.IdempotencyKey,
            receipt.SignatureKeyId,
            receipt.SignatureValue,
            receipt.CloudReference,
            receipt.OccurredAtUtc,
            receipt.PreparedAtUtc,
            receipt.ReceivedAtUtc);
    }

    private static ControlDeskEnvelopeReceiptEntity FromReceipt(ControlDeskEnvelopeReceipt receipt)
    {
        return new ControlDeskEnvelopeReceiptEntity
        {
            ReceiptId = receipt.ReceiptId,
            MessageId = receipt.MessageId,
            MessageType = receipt.MessageType,
            SubjectType = receipt.SubjectType,
            SubjectId = receipt.SubjectId,
            SourceSystem = receipt.SourceSystem,
            SourceEnvironment = receipt.SourceEnvironment,
            IdempotencyKey = receipt.IdempotencyKey,
            SignatureKeyId = receipt.SignatureKeyId,
            SignatureValue = receipt.SignatureValue,
            Status = receipt.Status.ToString(),
            CloudReference = receipt.CloudReference,
            OccurredAtUtc = receipt.OccurredAtUtc,
            PreparedAtUtc = receipt.PreparedAtUtc,
            ReceivedAtUtc = receipt.ReceivedAtUtc,
            Detail = receipt.Detail
        };
    }
}
