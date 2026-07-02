namespace SafarSuite.ControlDesk.Application.Modules.Payments.ReverseInvoicePayment;

public sealed record ReverseInvoicePaymentResult(
    Guid PaymentId,
    Guid InvoiceId,
    string InvoiceNumber,
    string InvoiceStatus,
    string PaymentStatus,
    decimal Amount,
    decimal BalanceDue,
    string CurrencyCode,
    Guid ReversalJournalEntryId,
    string ReversalJournalEntryStatus,
    DateOnly ReversalDate,
    Guid OriginalJournalEntryId,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyCollection<ReverseInvoicePaymentJournalLineResult> JournalLines);

public sealed record ReverseInvoicePaymentJournalLineResult(
    Guid LedgerAccountId,
    decimal Debit,
    decimal Credit,
    string? Description);
