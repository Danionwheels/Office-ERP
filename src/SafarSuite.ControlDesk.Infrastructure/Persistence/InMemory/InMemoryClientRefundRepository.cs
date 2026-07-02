using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryClientRefundRepository : IClientRefundRepository
{
    private readonly ConcurrentDictionary<Guid, ClientRefund> _refundsById = new();

    public Task AddAsync(ClientRefund refund, CancellationToken cancellationToken = default)
    {
        _refundsById.TryAdd(refund.Id.Value, refund);

        return Task.CompletedTask;
    }

    public Task<ClientRefund?> GetByIdAsync(
        ClientRefundId id,
        CancellationToken cancellationToken = default)
    {
        _refundsById.TryGetValue(id.Value, out var refund);

        return Task.FromResult(refund);
    }

    public Task<IReadOnlyCollection<ClientRefund>> ListForClientAsync(
        ClientId clientId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var refunds = _refundsById.Values
            .Where(refund => refund.ClientId == clientId);

        if (fromDate.HasValue)
        {
            refunds = refunds.Where(refund => refund.RefundedOn >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            refunds = refunds.Where(refund => refund.RefundedOn <= toDate.Value);
        }

        var sortedRefunds = refunds
            .OrderBy(refund => refund.RefundedOn)
            .ThenBy(refund => refund.CreatedAtUtc)
            .ThenBy(refund => refund.Id.Value)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ClientRefund>>(sortedRefunds);
    }

    public Task<bool> ExistsByReferenceAsync(
        ClientRefundReference reference,
        CancellationToken cancellationToken = default)
    {
        var exists = _refundsById.Values
            .Any(refund => string.Equals(
                refund.Reference.Value,
                reference.Value,
                StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(exists);
    }
}
