namespace SafarSuite.ControlDesk.Application.Modules.Billing.ListOutstandingInvoices;

public sealed record ListOutstandingInvoicesResult(
    IReadOnlyCollection<OutstandingInvoiceResult> Invoices,
    int PageSize,
    bool HasMore,
    string? NextCursor,
    long FilteredCount);

public sealed record OutstandingInvoiceResult(
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
