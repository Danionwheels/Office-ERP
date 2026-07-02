using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfClientRefundRepository : IClientRefundRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfClientRefundRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ClientRefund refund, CancellationToken cancellationToken = default)
    {
        await _dbContext.ClientRefunds.AddAsync(refund, cancellationToken);
    }

    public async Task<ClientRefund?> GetByIdAsync(
        ClientRefundId id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClientRefunds
            .SingleOrDefaultAsync(refund => refund.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ClientRefund>> ListForClientAsync(
        ClientId clientId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var refunds = _dbContext.ClientRefunds
            .Where(refund => refund.ClientId == clientId);

        if (fromDate.HasValue)
        {
            refunds = refunds.Where(refund => refund.RefundedOn >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            refunds = refunds.Where(refund => refund.RefundedOn <= toDate.Value);
        }

        return await refunds
            .OrderBy(refund => refund.RefundedOn)
            .ThenBy(refund => refund.CreatedAtUtc)
            .ThenBy(refund => refund.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<bool> ExistsByReferenceAsync(
        ClientRefundReference reference,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClientRefunds
            .AnyAsync(refund => refund.Reference == reference, cancellationToken);
    }
}
