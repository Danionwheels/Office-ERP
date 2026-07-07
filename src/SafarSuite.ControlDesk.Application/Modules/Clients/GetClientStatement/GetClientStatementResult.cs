namespace SafarSuite.ControlDesk.Application.Modules.Clients.GetClientStatement;

public sealed record GetClientStatementResult(
    Guid ClientId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    IReadOnlyCollection<ClientStatementCurrencySummaryResult> CurrencySummaries,
    IReadOnlyCollection<ClientStatementInvoiceResult> Invoices,
    IReadOnlyCollection<ClientStatementPaymentResult> Payments,
    IReadOnlyCollection<ClientStatementLineResult> Lines,
    IReadOnlyCollection<ClientStatementJournalPostingResult> JournalPostings);

public sealed record ClientStatementCurrencySummaryResult(
    string CurrencyCode,
    decimal TotalInvoiced,
    decimal TotalPaid,
    decimal AvailableCredit,
    decimal BalanceDue,
    int InvoiceCount,
    int OpenInvoiceCount);

public sealed record ClientStatementInvoiceResult(
    Guid InvoiceId,
    Guid ContractId,
    string InvoiceNumber,
    DateOnly IssueDate,
    DateOnly DueDate,
    string Status,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal BalanceDue,
    string CurrencyCode,
    Guid? JournalEntryId);

public sealed record ClientStatementPaymentResult(
    Guid PaymentId,
    Guid InvoiceId,
    string Reference,
    string Method,
    string Status,
    decimal Amount,
    string CurrencyCode,
    DateOnly ReceivedOn,
    Guid? JournalEntryId);

public sealed record ClientStatementLineResult(
    DateOnly EntryDate,
    string DocumentType,
    string Reference,
    Guid? InvoiceId,
    Guid? PaymentId,
    Guid? RefundId,
    Guid? CreditApplicationId,
    string Description,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance,
    string CurrencyCode,
    Guid? JournalEntryId);

public sealed record ClientStatementJournalPostingResult(
    Guid JournalEntryId,
    DateOnly EntryDate,
    string SourceType,
    string? SourceReference,
    string? Memo,
    string Status,
    decimal TotalDebit,
    decimal TotalCredit,
    string CurrencyCode,
    IReadOnlyCollection<ClientStatementJournalLineResult> Lines);

public sealed record ClientStatementJournalLineResult(
    Guid LedgerAccountId,
    string? LedgerAccountCode,
    string? LedgerAccountName,
    string? LedgerAccountType,
    string? LedgerAccountLevel,
    bool? IsPostingAccount,
    string? LedgerAccountStatus,
    decimal Debit,
    decimal Credit,
    string? Description);
