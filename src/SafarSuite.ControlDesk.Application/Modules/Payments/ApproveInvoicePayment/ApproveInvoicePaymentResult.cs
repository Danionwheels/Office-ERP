namespace SafarSuite.ControlDesk.Application.Modules.Payments.ApproveInvoicePayment;

public sealed record ApproveInvoicePaymentResult(
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
    IReadOnlyCollection<ApproveInvoicePaymentJournalLineResult> JournalLines);

public sealed record ApproveInvoicePaymentJournalLineResult(
    Guid LedgerAccountId,
    decimal Debit,
    decimal Credit,
    string? Description,
    string? LedgerAccountCode,
    string? LedgerAccountName,
    string? LedgerAccountType,
    string? LedgerAccountNormalBalance,
    string? LedgerAccountLevel,
    bool? IsPostingAccount,
    string? LedgerAccountStatus);
