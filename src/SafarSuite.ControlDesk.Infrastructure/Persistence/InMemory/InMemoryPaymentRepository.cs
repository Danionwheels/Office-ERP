using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryPaymentRepository : IPaymentRepository
{
    private readonly ConcurrentDictionary<Guid, Payment> _paymentsById = new();

    public Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _paymentsById.TryAdd(payment.Id.Value, payment);

        return Task.CompletedTask;
    }

    public Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken = default)
    {
        _paymentsById.TryGetValue(id.Value, out var payment);

        return Task.FromResult(payment);
    }

    public Task<Payment?> GetByPortalClaimIdAsync(
        PortalPaymentClaimId portalClaimId,
        CancellationToken cancellationToken = default)
    {
        var payment = _paymentsById.Values
            .SingleOrDefault(candidate => candidate.PortalClaimId == portalClaimId);

        return Task.FromResult(payment);
    }

    public Task<IReadOnlyCollection<Payment>> ListByReferenceAsync(
        PaymentReference reference,
        CancellationToken cancellationToken = default)
    {
        var payments = _paymentsById.Values
            .Where(payment => string.Equals(
                payment.Reference.Value,
                reference.Value,
                StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(payment => payment.RecordedAtUtc)
            .ThenByDescending(payment => payment.Id.Value)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<Payment>>(payments);
    }

    public Task<IReadOnlyCollection<Payment>> ListForClientAsync(
        ClientId clientId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var payments = _paymentsById.Values
            .Where(payment => payment.ClientId == clientId);

        if (fromDate.HasValue)
        {
            payments = payments.Where(payment => payment.ReceivedOn >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            payments = payments.Where(payment => payment.ReceivedOn <= toDate.Value);
        }

        var sortedPayments = payments
            .OrderBy(payment => payment.ReceivedOn)
            .ThenBy(payment => payment.RecordedAtUtc)
            .ThenBy(payment => payment.Id.Value)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<Payment>>(sortedPayments);
    }

    internal Payment[] Snapshot() => _paymentsById.Values.ToArray();
}
