namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Billing;

public sealed record AccountsReceivableAgingResponse(
    DateOnly AsOfDate,
    IReadOnlyCollection<AccountsReceivableAgingCurrencyResponse> Currencies,
    IReadOnlyCollection<AccountsReceivableAgingClientResponse> Clients);

public sealed record AccountsReceivableAgingCurrencyResponse(
    string CurrencyCode,
    decimal CurrentAmount,
    decimal Days1To30Amount,
    decimal Days31To60Amount,
    decimal Days61To90Amount,
    decimal DaysOver90Amount,
    decimal TotalOutstanding,
    long InvoiceCount,
    long ClientCount);

public sealed record AccountsReceivableAgingClientResponse(
    Guid ClientId,
    string ClientCode,
    string ClientName,
    string CurrencyCode,
    decimal CurrentAmount,
    decimal Days1To30Amount,
    decimal Days31To60Amount,
    decimal Days61To90Amount,
    decimal DaysOver90Amount,
    decimal TotalOutstanding,
    long InvoiceCount);

public sealed record OutstandingInvoicePageResponse(
    IReadOnlyCollection<OutstandingInvoiceResponse> Invoices,
    int PageSize,
    bool HasMore,
    string? NextCursor,
    long FilteredCount);

public sealed record OutstandingInvoiceResponse(
    Guid InvoiceId,
    Guid ClientId,
    string ClientCode,
    string ClientName,
    string InvoiceNumber,
    DateOnly IssueDate,
    DateOnly DueDate,
    string Status,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal BalanceDue,
    string CurrencyCode,
    int DaysOverdue,
    string AgingBucket,
    Guid? JournalEntryId);
