using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetJournalEntrySourceDocument;

public sealed class GetJournalEntrySourceDocumentHandler
{
    private readonly IJournalEntryRepository _journalEntries;
    private readonly IInvoiceRepository _invoices;
    private readonly ICreditNoteRepository _creditNotes;
    private readonly IPaymentRepository _payments;
    private readonly IClientRefundRepository _refunds;

    public GetJournalEntrySourceDocumentHandler(
        IJournalEntryRepository journalEntries,
        IInvoiceRepository invoices,
        ICreditNoteRepository creditNotes,
        IPaymentRepository payments,
        IClientRefundRepository refunds)
    {
        _journalEntries = journalEntries;
        _invoices = invoices;
        _creditNotes = creditNotes;
        _payments = payments;
        _refunds = refunds;
    }

    public async Task<Result<JournalEntrySourceDocumentResult>> HandleAsync(
        GetJournalEntrySourceDocumentQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.JournalEntryId == Guid.Empty)
        {
            return Result<JournalEntrySourceDocumentResult>.Failure(ApplicationError.Validation(
                nameof(query.JournalEntryId),
                "Journal entry id cannot be empty."));
        }

        var entry = await _journalEntries.GetByIdAsync(
            JournalEntryId.Create(query.JournalEntryId),
            cancellationToken);

        if (entry is null)
        {
            return Result<JournalEntrySourceDocumentResult>.Failure(ApplicationError.NotFound(
                nameof(query.JournalEntryId),
                "Journal entry was not found."));
        }

        if (string.IsNullOrWhiteSpace(entry.SourceReference))
        {
            return Result<JournalEntrySourceDocumentResult>.Success(Unresolved(
                entry,
                "Journal entry does not have a source reference."));
        }

        return entry.SourceType switch
        {
            JournalSourceType.BillingInvoice => Result<JournalEntrySourceDocumentResult>.Success(
                await ResolveInvoiceAsync(entry, isVoid: false, cancellationToken)),
            JournalSourceType.BillingInvoiceVoid => Result<JournalEntrySourceDocumentResult>.Success(
                await ResolveInvoiceAsync(entry, isVoid: true, cancellationToken)),
            JournalSourceType.BillingCreditNote => Result<JournalEntrySourceDocumentResult>.Success(
                await ResolveCreditNoteAsync(entry, cancellationToken)),
            JournalSourceType.PaymentReceipt => Result<JournalEntrySourceDocumentResult>.Success(
                await ResolvePaymentAsync(entry, isReversal: false, cancellationToken)),
            JournalSourceType.PaymentReversal => Result<JournalEntrySourceDocumentResult>.Success(
                await ResolvePaymentAsync(entry, isReversal: true, cancellationToken)),
            JournalSourceType.ClientRefund => Result<JournalEntrySourceDocumentResult>.Success(
                await ResolveRefundAsync(entry, cancellationToken)),
            _ => Result<JournalEntrySourceDocumentResult>.Success(Unresolved(
                entry,
                $"{entry.SourceType} journals do not have a linked source document."))
        };
    }

    private async Task<JournalEntrySourceDocumentResult> ResolveInvoiceAsync(
        JournalEntry entry,
        bool isVoid,
        CancellationToken cancellationToken)
    {
        if (!TryCreateInvoiceNumber(entry.SourceReference, out var invoiceNumber))
        {
            return Unresolved(entry, "Journal source reference is not a valid invoice number.");
        }

        var invoice = await _invoices.GetByNumberAsync(invoiceNumber, cancellationToken);

        if (invoice is null)
        {
            return Unresolved(entry, $"Invoice {invoiceNumber.Value} was not found.");
        }

        return Resolved(
            entry,
            "Invoice",
            invoice.Id.Value,
            invoice.ClientId.Value,
            relatedInvoiceId: null,
            invoice.Number.Value,
            invoice.Status.ToString(),
            invoice.IssueDate,
            invoice.CurrencyCode,
            invoice.TotalAmount.Amount,
            isVoid ? $"Voided invoice {invoice.Number.Value}" : $"Invoice {invoice.Number.Value}",
            "billing",
            "issue");
    }

    private async Task<JournalEntrySourceDocumentResult> ResolveCreditNoteAsync(
        JournalEntry entry,
        CancellationToken cancellationToken)
    {
        if (!TryCreateCreditNoteNumber(entry.SourceReference, out var creditNoteNumber))
        {
            return Unresolved(entry, "Journal source reference is not a valid credit note number.");
        }

        var creditNote = await _creditNotes.GetByNumberAsync(creditNoteNumber, cancellationToken);

        if (creditNote is null)
        {
            return Unresolved(entry, $"Credit note {creditNoteNumber.Value} was not found.");
        }

        return Resolved(
            entry,
            "CreditNote",
            creditNote.Id.Value,
            creditNote.ClientId.Value,
            creditNote.InvoiceId.Value,
            creditNote.Number.Value,
            creditNote.Status.ToString(),
            creditNote.CreditDate,
            creditNote.CurrencyCode,
            creditNote.TotalAmount.Amount,
            $"Credit note {creditNote.Number.Value}",
            "billing",
            "issue");
    }

    private async Task<JournalEntrySourceDocumentResult> ResolvePaymentAsync(
        JournalEntry entry,
        bool isReversal,
        CancellationToken cancellationToken)
    {
        if (!TryCreatePaymentReference(entry.SourceReference, out var paymentReference))
        {
            return Unresolved(entry, "Journal source reference is not a valid payment reference.");
        }

        var payments = await _payments.ListByReferenceAsync(paymentReference, cancellationToken);
        var payment = payments.FirstOrDefault(candidate =>
                candidate.Amount.Amount == entry.TotalDebit.Amount
                && string.Equals(candidate.Amount.CurrencyCode, entry.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            ?? payments.FirstOrDefault();

        if (payment is null)
        {
            return Unresolved(entry, $"Payment {paymentReference.Value} was not found.");
        }

        return Resolved(
            entry,
            "Payment",
            payment.Id.Value,
            payment.ClientId.Value,
            payment.InvoiceId.Value,
            payment.Reference.Value,
            payment.Status.ToString(),
            payment.ReceivedOn,
            payment.Amount.CurrencyCode,
            payment.Amount.Amount,
            isReversal ? $"Payment reversal {payment.Reference.Value}" : $"Payment {payment.Reference.Value}",
            "payments",
            "result");
    }

    private async Task<JournalEntrySourceDocumentResult> ResolveRefundAsync(
        JournalEntry entry,
        CancellationToken cancellationToken)
    {
        if (!TryCreateRefundReference(entry.SourceReference, out var refundReference))
        {
            return Unresolved(entry, "Journal source reference is not a valid refund reference.");
        }

        var refund = await _refunds.GetByReferenceAsync(refundReference, cancellationToken);

        if (refund is null)
        {
            return Unresolved(entry, $"Refund {refundReference.Value} was not found.");
        }

        return Resolved(
            entry,
            "ClientRefund",
            refund.Id.Value,
            refund.ClientId.Value,
            relatedInvoiceId: null,
            refund.Reference.Value,
            refund.Status.ToString(),
            refund.RefundedOn,
            refund.CurrencyCode,
            refund.Amount.Amount,
            $"Refund {refund.Reference.Value}",
            "payments",
            "refund");
    }

    private static JournalEntrySourceDocumentResult Resolved(
        JournalEntry entry,
        string documentKind,
        Guid documentId,
        Guid clientId,
        Guid? relatedInvoiceId,
        string reference,
        string status,
        DateOnly documentDate,
        string currencyCode,
        decimal amount,
        string label,
        string dashboardModule,
        string dashboardStep)
    {
        return new JournalEntrySourceDocumentResult(
            entry.Id.Value,
            entry.SourceType.ToString(),
            entry.SourceReference,
            IsResolved: true,
            documentKind,
            documentId,
            clientId,
            relatedInvoiceId,
            reference,
            status,
            documentDate,
            currencyCode,
            amount,
            label,
            dashboardModule,
            dashboardStep,
            Message: null);
    }

    private static JournalEntrySourceDocumentResult Unresolved(JournalEntry entry, string message)
    {
        return new JournalEntrySourceDocumentResult(
            entry.Id.Value,
            entry.SourceType.ToString(),
            entry.SourceReference,
            IsResolved: false,
            DocumentKind: null,
            DocumentId: null,
            ClientId: null,
            RelatedInvoiceId: null,
            Reference: entry.SourceReference,
            Status: null,
            DocumentDate: null,
            CurrencyCode: null,
            Amount: null,
            Label: null,
            DashboardModule: null,
            DashboardStep: null,
            message);
    }

    private static bool TryCreateInvoiceNumber(string? sourceReference, out InvoiceNumber number)
    {
        try
        {
            number = InvoiceNumber.Create(sourceReference ?? string.Empty);
            return true;
        }
        catch (ArgumentException)
        {
            number = null!;
            return false;
        }
    }

    private static bool TryCreateCreditNoteNumber(string? sourceReference, out CreditNoteNumber number)
    {
        try
        {
            number = CreditNoteNumber.Create(sourceReference ?? string.Empty);
            return true;
        }
        catch (ArgumentException)
        {
            number = null!;
            return false;
        }
    }

    private static bool TryCreatePaymentReference(string? sourceReference, out PaymentReference reference)
    {
        try
        {
            reference = PaymentReference.Create(sourceReference ?? string.Empty);
            return true;
        }
        catch (ArgumentException)
        {
            reference = null!;
            return false;
        }
    }

    private static bool TryCreateRefundReference(string? sourceReference, out ClientRefundReference reference)
    {
        try
        {
            reference = ClientRefundReference.Create(sourceReference ?? string.Empty);
            return true;
        }
        catch (ArgumentException)
        {
            reference = null!;
            return false;
        }
    }
}
