using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryCreditNoteRepository : ICreditNoteRepository
{
    private readonly ConcurrentDictionary<Guid, CreditNote> _creditNotesById = new();

    public Task AddAsync(CreditNote creditNote, CancellationToken cancellationToken = default)
    {
        _creditNotesById.TryAdd(creditNote.Id.Value, creditNote);

        return Task.CompletedTask;
    }

    public Task<CreditNote?> GetByIdAsync(CreditNoteId id, CancellationToken cancellationToken = default)
    {
        _creditNotesById.TryGetValue(id.Value, out var creditNote);

        return Task.FromResult(creditNote);
    }

    public Task<IReadOnlyCollection<CreditNote>> ListForClientAsync(
        ClientId clientId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var creditNotes = _creditNotesById.Values
            .Where(creditNote => creditNote.ClientId == clientId);

        if (fromDate.HasValue)
        {
            creditNotes = creditNotes.Where(creditNote => creditNote.CreditDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            creditNotes = creditNotes.Where(creditNote => creditNote.CreditDate <= toDate.Value);
        }

        var sortedCreditNotes = creditNotes
            .OrderBy(creditNote => creditNote.CreditDate)
            .ThenBy(creditNote => creditNote.CreatedAtUtc)
            .ThenBy(creditNote => creditNote.Id.Value)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CreditNote>>(sortedCreditNotes);
    }

    public Task<bool> ExistsByNumberAsync(CreditNoteNumber number, CancellationToken cancellationToken = default)
    {
        var exists = _creditNotesById.Values.Any(creditNote => creditNote.Number.Equals(number));

        return Task.FromResult(exists);
    }

    public Task<bool> ExistsForInvoiceAsync(InvoiceId invoiceId, CancellationToken cancellationToken = default)
    {
        var exists = _creditNotesById.Values.Any(creditNote => creditNote.InvoiceId == invoiceId);

        return Task.FromResult(exists);
    }
}
