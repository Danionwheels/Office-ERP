using SafarSuite.ControlDesk.Api.Common;
using SafarSuite.ControlDesk.Application.Modules.Payments.RecordInvoicePayment;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Payments;

namespace SafarSuite.ControlDesk.Api.Modules.Payments;

public static class PaymentsEndpoints
{
    public static IEndpointRouteBuilder MapPaymentsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/payments")
            .WithTags("Payments");

        group.MapPost("/invoice-payments", RecordInvoicePaymentAsync);

        return endpoints;
    }

    private static async Task<IResult> RecordInvoicePaymentAsync(
        RecordInvoicePaymentRequest request,
        RecordInvoicePaymentHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new RecordInvoicePaymentCommand(
            request.InvoiceId,
            request.Method,
            request.Reference,
            request.Amount,
            request.CurrencyCode,
            request.ReceivedOn,
            request.CashOrBankAccountId,
            request.AccountsReceivableAccountId,
            request.PostingDate);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new RecordInvoicePaymentResponse(
            result.Value.PaymentId,
            result.Value.InvoiceId,
            result.Value.InvoiceNumber,
            result.Value.InvoiceStatus,
            result.Value.PaymentStatus,
            result.Value.Amount,
            result.Value.BalanceDue,
            result.Value.CurrencyCode,
            result.Value.JournalEntryId,
            result.Value.JournalEntryStatus,
            result.Value.PostingDate,
            result.Value.TotalDebit,
            result.Value.TotalCredit,
            result.Value.JournalLines.Select(line => new RecordInvoicePaymentJournalLineResponse(
                line.LedgerAccountId,
                line.Debit,
                line.Credit,
                line.Description)).ToArray());

        return Results.Created($"/api/v1/payments/invoice-payments/{response.PaymentId}", response);
    }
}
