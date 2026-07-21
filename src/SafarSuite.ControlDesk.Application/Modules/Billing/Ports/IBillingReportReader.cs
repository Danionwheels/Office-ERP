namespace SafarSuite.ControlDesk.Application.Modules.Billing.Ports;

public interface IBillingReportReader
{
    Task<IReadOnlyCollection<AccountsReceivableAgingClientReadModel>> ReadAccountsReceivableAgingAsync(
        AccountsReceivableAgingReadRequest request,
        CancellationToken cancellationToken = default);

    Task<OutstandingInvoiceReadPage> ReadOutstandingInvoicePageAsync(
        OutstandingInvoiceReadRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record AccountsReceivableAgingReadRequest(
    DateOnly AsOfDate,
    string CurrencyCode);

public sealed record AccountsReceivableAgingClientReadModel(
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

public sealed record OutstandingInvoiceReadRequest(
    Guid? ClientId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    decimal? MinAmount,
    decimal? MaxAmount,
    string Status,
    string? CurrencyCode,
    DateOnly Today,
    DateOnly? AfterIssueDate,
    DateTimeOffset? AfterCreatedAtUtc,
    Guid? AfterInvoiceId,
    int Take);

public sealed record OutstandingInvoiceReadPage(
    IReadOnlyCollection<OutstandingInvoiceReadItem> Items,
    long FilteredCount);

public sealed record OutstandingInvoiceReadItem(
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
    Guid? JournalEntryId,
    DateTimeOffset CreatedAtUtc);
