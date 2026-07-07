using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Billing.Common;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Billing;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.GetCreditNoteDocument;

public sealed class GetCreditNoteDocumentHandler
{
    private readonly ICreditNoteRepository _creditNotes;
    private readonly IInvoiceRepository _invoices;
    private readonly IJournalEntryRepository _journalEntries;
    private readonly ILedgerAccountRepository _ledgerAccounts;

    public GetCreditNoteDocumentHandler(
        ICreditNoteRepository creditNotes,
        IInvoiceRepository invoices,
        IJournalEntryRepository journalEntries,
        ILedgerAccountRepository ledgerAccounts)
    {
        _creditNotes = creditNotes;
        _invoices = invoices;
        _journalEntries = journalEntries;
        _ledgerAccounts = ledgerAccounts;
    }

    public async Task<Result<CreditNoteDocumentResult>> HandleAsync(
        GetCreditNoteDocumentQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.CreditNoteId == Guid.Empty)
        {
            return Result<CreditNoteDocumentResult>.Failure(ApplicationError.Validation(
                nameof(query.CreditNoteId),
                "Credit note id cannot be empty."));
        }

        var creditNote = await _creditNotes.GetByIdAsync(
            CreditNoteId.Create(query.CreditNoteId),
            cancellationToken);

        if (creditNote is null)
        {
            return Result<CreditNoteDocumentResult>.Failure(ApplicationError.NotFound(
                nameof(query.CreditNoteId),
                "Credit note was not found."));
        }

        var invoice = await _invoices.GetByIdAsync(creditNote.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            return Result<CreditNoteDocumentResult>.Failure(ApplicationError.NotFound(
                nameof(creditNote.InvoiceId),
                "Invoice for this credit note was not found."));
        }

        var journalEntry = await FindJournalAsync(creditNote, cancellationToken);

        if (journalEntry is null)
        {
            return Result<CreditNoteDocumentResult>.Failure(ApplicationError.NotFound(
                nameof(query.CreditNoteId),
                "Credit note journal entry was not found."));
        }
        var ledgerAccountsById = JournalLineLedgerAccountMetadataFactory.ToLookup(
            await _ledgerAccounts.ListAsync(cancellationToken: cancellationToken));

        return Result<CreditNoteDocumentResult>.Success(new CreditNoteDocumentResult(
            BillingDocumentResultFactory.ToInvoiceDraftResult(invoice),
            BillingDocumentResultFactory.ToIssueCreditNoteResult(
                creditNote,
                invoice,
                journalEntry,
                ledgerAccountsById)));
    }

    private async Task<JournalEntry?> FindJournalAsync(
        CreditNote creditNote,
        CancellationToken cancellationToken)
    {
        var entries = await _journalEntries.ListAsync(
            sourceType: JournalSourceType.BillingCreditNote,
            cancellationToken: cancellationToken);

        return entries
            .Where(entry => string.Equals(entry.SourceReference, creditNote.Number.Value, StringComparison.OrdinalIgnoreCase))
            .Where(entry => string.Equals(entry.CurrencyCode, creditNote.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            .Where(entry => entry.TotalDebit.Amount == creditNote.TotalAmount.Amount)
            .OrderBy(entry => entry.EntryDate)
            .ThenBy(entry => entry.CreatedAtUtc)
            .FirstOrDefault();
    }
}
