namespace SafarSuite.ControlDesk.Application.Modules.Clients.Financials;

public sealed record ClientFinancialSummaryResult(
    Guid ClientId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    IReadOnlyCollection<ClientFinancialCurrencySummaryResult> CurrencySummaries);

public sealed record ClientFinancialCurrencySummaryResult(
    string CurrencyCode,
    decimal TotalInvoiced,
    decimal TotalPaid,
    decimal AvailableCredit,
    decimal BalanceDue,
    long InvoiceCount,
    long OpenInvoiceCount);

public sealed record ClientInvoicePageResult(
    IReadOnlyCollection<ClientInvoiceRegisterItemResult> Invoices,
    int PageSize,
    bool HasMore,
    string? NextCursor,
    long FilteredCount);

public sealed record ClientInvoiceRegisterItemResult(
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

public sealed record ClientPaymentPageResult(
    IReadOnlyCollection<ClientPaymentRegisterItemResult> Payments,
    int PageSize,
    bool HasMore,
    string? NextCursor,
    long FilteredCount);

public sealed record ClientPaymentRegisterItemResult(
    Guid PaymentId,
    Guid InvoiceId,
    string Reference,
    string Method,
    string Status,
    decimal Amount,
    string CurrencyCode,
    DateOnly ReceivedOn,
    Guid? JournalEntryId);

public sealed record ClientFinancialActivityPageResult(
    IReadOnlyCollection<ClientFinancialActivityItemResult> Lines,
    int PageSize,
    bool HasMore,
    string? NextCursor,
    long FilteredCount);

public sealed record ClientFinancialActivityItemResult(
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

public sealed record ClientJournalPostingPageResult(
    IReadOnlyCollection<ClientJournalPostingItemResult> JournalPostings,
    int PageSize,
    bool HasMore,
    string? NextCursor,
    long FilteredCount);

public sealed record ClientJournalPostingItemResult(
    Guid JournalEntryId,
    DateOnly EntryDate,
    string SourceType,
    string? SourceReference,
    string? Memo,
    string Status,
    decimal TotalDebit,
    decimal TotalCredit,
    string CurrencyCode,
    int LineCount);
