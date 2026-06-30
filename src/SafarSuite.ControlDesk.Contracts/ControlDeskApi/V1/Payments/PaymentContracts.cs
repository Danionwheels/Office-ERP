namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Payments;

public sealed record RecordInvoicePaymentRequest(
    Guid InvoiceId,
    string Method,
    string Reference,
    decimal Amount,
    string CurrencyCode,
    DateOnly ReceivedOn,
    Guid CashOrBankAccountId,
    Guid AccountsReceivableAccountId,
    DateOnly PostingDate);

public sealed record RecordInvoicePaymentResponse(
    Guid PaymentId,
    Guid InvoiceId,
    string InvoiceNumber,
    string InvoiceStatus,
    string PaymentStatus,
    decimal Amount,
    decimal BalanceDue,
    string CurrencyCode,
    Guid JournalEntryId,
    string JournalEntryStatus,
    DateOnly PostingDate,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyCollection<RecordInvoicePaymentJournalLineResponse> JournalLines);

public sealed record RecordInvoicePaymentJournalLineResponse(
    Guid LedgerAccountId,
    decimal Debit,
    decimal Credit,
    string? Description);
