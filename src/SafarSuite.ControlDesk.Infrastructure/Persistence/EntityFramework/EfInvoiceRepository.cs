using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfInvoiceRepository : IInvoiceRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfInvoiceRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await _dbContext.Invoices.AddAsync(invoice, cancellationToken);
    }

    public async Task<Invoice?> GetByIdAsync(InvoiceId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices
            .Include(invoice => invoice.Lines)
            .SingleOrDefaultAsync(invoice => invoice.Id == id, cancellationToken);
    }

    public async Task<Invoice?> GetByNumberAsync(
        InvoiceNumber number,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices
            .Include(invoice => invoice.Lines)
            .SingleOrDefaultAsync(invoice => invoice.Number == number, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Invoice>> ListForClientAsync(
        ClientId clientId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var invoices = _dbContext.Invoices
            .Include(invoice => invoice.Lines)
            .Where(invoice => invoice.ClientId == clientId);

        if (fromDate.HasValue)
        {
            invoices = invoices.Where(invoice => invoice.IssueDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            invoices = invoices.Where(invoice => invoice.IssueDate <= toDate.Value);
        }

        return await invoices
            .OrderBy(invoice => invoice.IssueDate)
            .ThenBy(invoice => invoice.CreatedAtUtc)
            .ThenBy(invoice => invoice.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNumberAsync(InvoiceNumber number, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices
            .AnyAsync(invoice => invoice.Number == number, cancellationToken);
    }
}
