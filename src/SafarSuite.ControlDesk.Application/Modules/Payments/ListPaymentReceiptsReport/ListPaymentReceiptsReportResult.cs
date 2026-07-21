namespace SafarSuite.ControlDesk.Application.Modules.Payments.ListPaymentReceiptsReport;

public sealed record ListPaymentReceiptsReportResult(
    IReadOnlyCollection<PaymentReceiptReportItemResult> Payments,
    int PageSize,
    bool HasMore,
    string? NextCursor,
    long FilteredCount);

public sealed record PaymentReceiptReportItemResult(
    Guid PaymentId,
    Guid ClientId,
    string ClientCode,
    string ClientName,
    Guid InvoiceId,
    string InvoiceNumber,
    string Reference,
    string Method,
    string Status,
    decimal Amount,
    string CurrencyCode,
    DateOnly ReceivedOn,
    Guid? JournalEntryId);
