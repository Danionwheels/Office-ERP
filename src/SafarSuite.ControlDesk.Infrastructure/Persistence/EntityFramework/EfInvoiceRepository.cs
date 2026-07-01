using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Billing;

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

    public async Task<bool> ExistsByNumberAsync(InvoiceNumber number, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices
            .AnyAsync(invoice => invoice.Number == number, cancellationToken);
    }
}
