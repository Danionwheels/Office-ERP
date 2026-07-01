using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

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

    public Task<IReadOnlyCollection<Invoice>> ListForClientAsync(
        ClientId clientId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var invoices = _invoicesById.Values
            .Where(invoice => invoice.ClientId == clientId);

        if (fromDate.HasValue)
        {
            invoices = invoices.Where(invoice => invoice.IssueDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            invoices = invoices.Where(invoice => invoice.IssueDate <= toDate.Value);
        }

        var sortedInvoices = invoices
            .OrderBy(invoice => invoice.IssueDate)
            .ThenBy(invoice => invoice.CreatedAtUtc)
            .ThenBy(invoice => invoice.Id.Value)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<Invoice>>(sortedInvoices);
    }

    public Task<bool> ExistsByNumberAsync(InvoiceNumber number, CancellationToken cancellationToken = default)
    {
        var exists = _invoicesById.Values.Any(invoice => invoice.Number.Equals(number));

        return Task.FromResult(exists);
    }
}
