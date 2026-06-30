using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
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
}
