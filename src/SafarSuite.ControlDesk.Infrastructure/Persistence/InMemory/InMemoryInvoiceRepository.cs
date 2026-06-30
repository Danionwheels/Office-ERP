using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Billing;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryInvoiceRepository : IInvoiceRepository
{
    private readonly ConcurrentDictionary<Guid, Invoice> _invoicesById = new();

    public Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        _invoicesById.TryAdd(invoice.Id.Value, invoice);

        return Task.CompletedTask;
    }

    public Task<Invoice?> GetByIdAsync(InvoiceId id, CancellationToken cancellationToken = default)
    {
        _invoicesById.TryGetValue(id.Value, out var invoice);

        return Task.FromResult(invoice);
    }

    public Task<bool> ExistsByNumberAsync(InvoiceNumber number, CancellationToken cancellationToken = default)
    {
        var exists = _invoicesById.Values.Any(invoice => invoice.Number.Equals(number));

        return Task.FromResult(exists);
    }
}
