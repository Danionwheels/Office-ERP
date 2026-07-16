using SafarSuite.ControlDesk.Api.Common;
using SafarSuite.ControlDesk.Application.Modules.Payments.ListPaymentReceiptsReport;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Payments;

namespace SafarSuite.ControlDesk.Api.Modules.Payments;

public static class PaymentReportEndpoints
{
    public static IEndpointRouteBuilder MapPaymentReportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/payments/reports")
            .WithTags("Payment Reports");

        group.MapGet("/receipts", ListPaymentReceiptsAsync);

        return endpoints;
    }

    private static async Task<IResult> ListPaymentReceiptsAsync(
        Guid? clientId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? method,
        string? status,
        string? currencyCode,
        int? take,
        string? cursor,
        ListPaymentReceiptsReportHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListPaymentReceiptsReportQuery(
                clientId,
                fromDate,
                toDate,
                method,
                status,
                currencyCode,
                take ?? 25,
                cursor),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new PaymentReceiptsReportResponse(
            result.Value.Payments.Select(payment => new PaymentReceiptReportItemResponse(
                payment.PaymentId,
                payment.ClientId,
                payment.ClientCode,
                payment.ClientName,
                payment.InvoiceId,
                payment.InvoiceNumber,
                payment.Reference,
                payment.Method,
                payment.Status,
                payment.Amount,
                payment.CurrencyCode,
                payment.ReceivedOn,
                payment.JournalEntryId)).ToArray(),
            result.Value.PageSize,
            result.Value.HasMore,
            result.Value.NextCursor,
            result.Value.FilteredCount));
    }
}
