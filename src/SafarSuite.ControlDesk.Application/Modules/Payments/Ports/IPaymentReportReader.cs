namespace SafarSuite.ControlDesk.Application.Modules.Payments.Ports;

public interface IPaymentReportReader
{
    Task<PaymentReceiptReportReadPage> ReadReceiptsPageAsync(
        PaymentReceiptReportReadRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record PaymentReceiptReportReadRequest(
    Guid? ClientId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? Method,
    string? Status,
    string? CurrencyCode,
    DateOnly? AfterReceivedOn,
    DateTimeOffset? AfterRecordedAtUtc,
    Guid? AfterPaymentId,
    int Take);

public sealed record PaymentReceiptReportReadPage(
    IReadOnlyCollection<PaymentReceiptReportReadItem> Items,
    long FilteredCount);

public sealed record PaymentReceiptReportReadItem(
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
    Guid? JournalEntryId,
    DateTimeOffset RecordedAtUtc);
