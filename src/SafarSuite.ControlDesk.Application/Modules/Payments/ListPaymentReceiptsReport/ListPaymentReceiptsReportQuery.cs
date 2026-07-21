namespace SafarSuite.ControlDesk.Application.Modules.Payments.ListPaymentReceiptsReport;

public sealed record ListPaymentReceiptsReportQuery(
    Guid? ClientId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    string? Method = null,
    string? Status = null,
    string? CurrencyCode = null,
    int Take = 25,
    string? Cursor = null);
