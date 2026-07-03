using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfCreditNoteRepository : ICreditNoteRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfCreditNoteRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(CreditNote creditNote, CancellationToken cancellationToken = default)
    {
        await _dbContext.CreditNotes.AddAsync(creditNote, cancellationToken);
    }

    public async Task<CreditNote?> GetByIdAsync(CreditNoteId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CreditNotes
            .SingleOrDefaultAsync(creditNote => creditNote.Id == id, cancellationToken);
    }

    public async Task<CreditNote?> GetByNumberAsync(
        CreditNoteNumber number,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CreditNotes
            .SingleOrDefaultAsync(creditNote => creditNote.Number == number, cancellationToken);
    }

    public async Task<CreditNote?> GetForInvoiceAsync(
        InvoiceId invoiceId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CreditNotes
            .SingleOrDefaultAsync(creditNote => creditNote.InvoiceId == invoiceId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CreditNote>> ListForClientAsync(
        ClientId clientId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var creditNotes = _dbContext.CreditNotes
            .Where(creditNote => creditNote.ClientId == clientId);

        if (fromDate.HasValue)
        {
            creditNotes = creditNotes.Where(creditNote => creditNote.CreditDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            creditNotes = creditNotes.Where(creditNote => creditNote.CreditDate <= toDate.Value);
        }

        return await creditNotes
            .OrderBy(creditNote => creditNote.CreditDate)
            .ThenBy(creditNote => creditNote.CreatedAtUtc)
            .ThenBy(creditNote => creditNote.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNumberAsync(CreditNoteNumber number, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CreditNotes
            .AnyAsync(creditNote => creditNote.Number == number, cancellationToken);
    }

    public async Task<bool> ExistsForInvoiceAsync(InvoiceId invoiceId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CreditNotes
            .AnyAsync(creditNote => creditNote.InvoiceId == invoiceId, cancellationToken);
    }
}
