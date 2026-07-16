namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Payments;

public sealed record PaymentReceiptsReportResponse(
    IReadOnlyCollection<PaymentReceiptReportItemResponse> Payments,
    int PageSize,
    bool HasMore,
    string? NextCursor,
    long FilteredCount);

public sealed record PaymentReceiptReportItemResponse(
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
