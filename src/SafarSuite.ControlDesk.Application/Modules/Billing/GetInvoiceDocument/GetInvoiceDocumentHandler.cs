using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Billing.Common;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Billing;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.GetInvoiceDocument;

public sealed class GetInvoiceDocumentHandler
{
    private readonly IInvoiceRepository _invoices;
    private readonly ICreditNoteRepository _creditNotes;
    private readonly IJournalEntryRepository _journalEntries;
    private readonly ILedgerAccountRepository _ledgerAccounts;

    public GetInvoiceDocumentHandler(
        IInvoiceRepository invoices,
        ICreditNoteRepository creditNotes,
        IJournalEntryRepository journalEntries,
        ILedgerAccountRepository ledgerAccounts)
    {
        _invoices = invoices;
        _creditNotes = creditNotes;
        _journalEntries = journalEntries;
        _ledgerAccounts = ledgerAccounts;
    }

    public async Task<Result<InvoiceDocumentResult>> HandleAsync(
        GetInvoiceDocumentQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.InvoiceId == Guid.Empty)
        {
            return Result<InvoiceDocumentResult>.Failure(ApplicationError.Validation(
                nameof(query.InvoiceId),
                "Invoice id cannot be empty."));
        }

        var invoice = await _invoices.GetByIdAsync(InvoiceId.Create(query.InvoiceId), cancellationToken);

        if (invoice is null)
        {
            return Result<InvoiceDocumentResult>.Failure(ApplicationError.NotFound(
                nameof(query.InvoiceId),
                "Invoice was not found."));
        }

        var issueJournal = await FindJournalAsync(
            JournalSourceType.BillingInvoice,
            invoice.Id.Value,
            invoice.CurrencyCode,
            invoice.TotalAmount.Amount,
            cancellationToken);
        var voidJournal = await FindJournalAsync(
            JournalSourceType.BillingInvoiceVoid,
            invoice.Id.Value,
            invoice.CurrencyCode,
            invoice.TotalAmount.Amount,
            cancellationToken);
        var creditNote = await _creditNotes.GetForInvoiceAsync(invoice.Id, cancellationToken);
        var creditNoteJournal = creditNote is null
            ? null
            : await FindJournalAsync(
                JournalSourceType.BillingCreditNote,
                creditNote.Id.Value,
                creditNote.CurrencyCode,
                creditNote.TotalAmount.Amount,
                cancellationToken);
        var ledgerAccountsById = JournalLineLedgerAccountMetadataFactory.ToLookup(
            await _ledgerAccounts.ListAsync(cancellationToken: cancellationToken));

        return Result<InvoiceDocumentResult>.Success(new InvoiceDocumentResult(
            BillingDocumentResultFactory.ToInvoiceDraftResult(invoice),
            issueJournal is null
                ? null
                : BillingDocumentResultFactory.ToIssueInvoiceResult(invoice, issueJournal, ledgerAccountsById),
            issueJournal is null || voidJournal is null
                ? null
                : BillingDocumentResultFactory.ToVoidInvoiceResult(
                    invoice,
                    issueJournal,
                    voidJournal,
                    ledgerAccountsById),
            creditNote is null || creditNoteJournal is null
                ? null
                : BillingDocumentResultFactory.ToIssueCreditNoteResult(
                    creditNote,
                    invoice,
                    creditNoteJournal,
                    ledgerAccountsById)));
    }

    private async Task<JournalEntry?> FindJournalAsync(
        JournalSourceType sourceType,
        Guid sourceDocumentId,
        string currencyCode,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var entries = await _journalEntries.ListForSourceDocumentAsync(
            sourceType,
            sourceDocumentId,
            cancellationToken);

        return entries
            .Where(entry => string.Equals(entry.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
            .Where(entry => entry.TotalDebit.Amount == amount)
            .OrderBy(entry => entry.EntryDate)
            .ThenBy(entry => entry.CreatedAtUtc)
            .FirstOrDefault();
    }
}
