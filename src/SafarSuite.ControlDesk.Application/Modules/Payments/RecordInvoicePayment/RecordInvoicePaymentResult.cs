namespace SafarSuite.ControlDesk.Application.Modules.Payments.RecordInvoicePayment;

public sealed record RecordInvoicePaymentResult(
    Guid PaymentId,
    Guid InvoiceId,
    string InvoiceNumber,
    string InvoiceStatus,
    string PaymentStatus,
    decimal Amount,
    decimal BalanceDue,
    string CurrencyCode,
    Guid? JournalEntryId,
    string? JournalEntryStatus,
    DateOnly? PostingDate,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyCollection<RecordInvoicePaymentJournalLineResult> JournalLines);

public sealed record RecordInvoicePaymentJournalLineResult(
    Guid LedgerAccountId,
    decimal Debit,
    decimal Credit,
    string? Description);
