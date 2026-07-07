using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Billing.Common;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.Common;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.GetInvoicePaymentDocument;

public sealed class GetInvoicePaymentDocumentHandler
{
    private readonly IPaymentRepository _payments;
    private readonly IInvoiceRepository _invoices;
    private readonly IJournalEntryRepository _journalEntries;
    private readonly ILedgerAccountRepository _ledgerAccounts;

    public GetInvoicePaymentDocumentHandler(
        IPaymentRepository payments,
        IInvoiceRepository invoices,
        IJournalEntryRepository journalEntries,
        ILedgerAccountRepository ledgerAccounts)
    {
        _payments = payments;
        _invoices = invoices;
        _journalEntries = journalEntries;
        _ledgerAccounts = ledgerAccounts;
    }

    public async Task<Result<InvoicePaymentDocumentResult>> HandleAsync(
        GetInvoicePaymentDocumentQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.PaymentId == Guid.Empty)
        {
            return Result<InvoicePaymentDocumentResult>.Failure(ApplicationError.Validation(
                nameof(query.PaymentId),
                "Payment id cannot be empty."));
        }

        var payment = await _payments.GetByIdAsync(PaymentId.Create(query.PaymentId), cancellationToken);

        if (payment is null)
        {
            return Result<InvoicePaymentDocumentResult>.Failure(ApplicationError.NotFound(
                nameof(query.PaymentId),
                "Payment was not found."));
        }

        var invoice = await _invoices.GetByIdAsync(payment.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            return Result<InvoicePaymentDocumentResult>.Failure(ApplicationError.NotFound(
                nameof(payment.InvoiceId),
                "Invoice for this payment was not found."));
        }

        var receiptJournal = await FindJournalAsync(
            JournalSourceType.PaymentReceipt,
            payment,
            cancellationToken);
        var reversalJournal = await FindJournalAsync(
            JournalSourceType.PaymentReversal,
            payment,
            cancellationToken);
        var ledgerAccountsById = JournalLineLedgerAccountMetadataFactory.ToLookup(
            await _ledgerAccounts.ListAsync(cancellationToken: cancellationToken));

        return Result<InvoicePaymentDocumentResult>.Success(new InvoicePaymentDocumentResult(
            BillingDocumentResultFactory.ToInvoiceDraftResult(invoice),
            PaymentDocumentResultFactory.ToRecordInvoicePaymentResult(
                payment,
                invoice,
                receiptJournal,
                ledgerAccountsById),
            receiptJournal is null || reversalJournal is null
                ? null
                : PaymentDocumentResultFactory.ToReverseInvoicePaymentResult(
                    payment,
                    invoice,
                    reversalJournal,
                    receiptJournal,
                    ledgerAccountsById)));
    }

    private async Task<JournalEntry?> FindJournalAsync(
        JournalSourceType sourceType,
        Payment payment,
        CancellationToken cancellationToken)
    {
        var entries = await _journalEntries.ListAsync(
            sourceType: sourceType,
            cancellationToken: cancellationToken);

        return entries
            .Where(entry => string.Equals(entry.SourceReference, payment.Reference.Value, StringComparison.OrdinalIgnoreCase))
            .Where(entry => string.Equals(entry.CurrencyCode, payment.Amount.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            .Where(entry => entry.TotalDebit.Amount == payment.Amount.Amount)
            .OrderBy(entry => entry.EntryDate)
            .ThenBy(entry => entry.CreatedAtUtc)
            .FirstOrDefault();
    }
}
