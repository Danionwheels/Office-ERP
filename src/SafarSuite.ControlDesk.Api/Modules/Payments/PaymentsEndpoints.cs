using SafarSuite.ControlDesk.Api.Common;
using SafarSuite.ControlDesk.Application.Modules.Payments.ApplyClientCredit;
using SafarSuite.ControlDesk.Application.Modules.Payments.ApproveInvoicePayment;
using SafarSuite.ControlDesk.Application.Modules.Payments.GetClientRefundDocument;
using SafarSuite.ControlDesk.Application.Modules.Payments.GetInvoicePaymentDocument;
using SafarSuite.ControlDesk.Application.Modules.Payments.IssueClientRefund;
using SafarSuite.ControlDesk.Application.Modules.Payments.RejectInvoicePayment;
using SafarSuite.ControlDesk.Application.Modules.Payments.RecordInvoicePayment;
using SafarSuite.ControlDesk.Application.Modules.Payments.ReverseInvoicePayment;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Billing;
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
        group.MapGet("/invoice-payments/{paymentId:guid}", GetInvoicePaymentDocumentAsync);
        group.MapPost("/invoice-payments/{paymentId:guid}/approve", ApproveInvoicePaymentAsync);
        group.MapPost("/invoice-payments/{paymentId:guid}/reject", RejectInvoicePaymentAsync);
        group.MapPost("/invoice-payments/{paymentId:guid}/reverse", ReverseInvoicePaymentAsync);
        group.MapPost("/client-refunds", IssueClientRefundAsync);
        group.MapGet("/client-refunds/{refundId:guid}", GetClientRefundDocumentAsync);
        group.MapPost("/client-credit-applications", ApplyClientCreditAsync);

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

    private static async Task<IResult> GetInvoicePaymentDocumentAsync(
        Guid paymentId,
        GetInvoicePaymentDocumentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetInvoicePaymentDocumentQuery(paymentId), cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new InvoicePaymentDocumentResponse(
            ToResponse(result.Value.Invoice),
            ToResponse(result.Value.Payment),
            result.Value.Reversal is null ? null : ToResponse(result.Value.Reversal)));
    }

    private static async Task<IResult> ApproveInvoicePaymentAsync(
        Guid paymentId,
        ApproveInvoicePaymentRequest request,
        ApproveInvoicePaymentHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveInvoicePaymentCommand(
            paymentId,
            request.CashOrBankAccountId,
            request.AccountsReceivableAccountId,
            request.PostingDate,
            request.DecisionNote);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new ApproveInvoicePaymentResponse(
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
            result.Value.JournalLines.Select(line => new ApproveInvoicePaymentJournalLineResponse(
                line.LedgerAccountId,
                line.Debit,
                line.Credit,
                line.Description)).ToArray());

        return Results.Ok(response);
    }

    private static async Task<IResult> RejectInvoicePaymentAsync(
        Guid paymentId,
        RejectInvoicePaymentRequest request,
        RejectInvoicePaymentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new RejectInvoicePaymentCommand(paymentId, request.DecisionNote),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new RejectInvoicePaymentResponse(
            result.Value.PaymentId,
            result.Value.InvoiceId,
            result.Value.PaymentStatus,
            result.Value.DecisionNote));
    }

    private static async Task<IResult> ReverseInvoicePaymentAsync(
        Guid paymentId,
        ReverseInvoicePaymentRequest request,
        ReverseInvoicePaymentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ReverseInvoicePaymentCommand(paymentId, request.ReversalDate, request.DecisionNote),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new ReverseInvoicePaymentResponse(
            result.Value.PaymentId,
            result.Value.InvoiceId,
            result.Value.InvoiceNumber,
            result.Value.InvoiceStatus,
            result.Value.PaymentStatus,
            result.Value.Amount,
            result.Value.BalanceDue,
            result.Value.CurrencyCode,
            result.Value.ReversalJournalEntryId,
            result.Value.ReversalJournalEntryStatus,
            result.Value.ReversalDate,
            result.Value.OriginalJournalEntryId,
            result.Value.TotalDebit,
            result.Value.TotalCredit,
            result.Value.JournalLines.Select(line => new ReverseInvoicePaymentJournalLineResponse(
                line.LedgerAccountId,
                line.Debit,
                line.Credit,
                line.Description)).ToArray());

        return Results.Ok(response);
    }

    private static async Task<IResult> IssueClientRefundAsync(
        IssueClientRefundRequest request,
        IssueClientRefundHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new IssueClientRefundCommand(
                request.ClientId,
                request.Method,
                request.Reference,
                request.Amount,
                request.CurrencyCode,
                request.RefundedOn,
                request.CashOrBankAccountId,
                request.AccountsReceivableAccountId,
                request.PostingDate,
                request.Note),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new IssueClientRefundResponse(
            result.Value.RefundId,
            result.Value.ClientId,
            result.Value.RefundStatus,
            result.Value.Method,
            result.Value.Reference,
            result.Value.Amount,
            result.Value.ClientBalanceBefore,
            result.Value.ClientBalanceAfter,
            result.Value.CurrencyCode,
            result.Value.RefundedOn,
            result.Value.JournalEntryId,
            result.Value.JournalEntryStatus,
            result.Value.PostingDate,
            result.Value.TotalDebit,
            result.Value.TotalCredit,
            result.Value.JournalLines.Select(line => new IssueClientRefundJournalLineResponse(
                line.LedgerAccountId,
                line.Debit,
                line.Credit,
                line.Description)).ToArray());

        return Results.Created($"/api/v1/payments/client-refunds/{response.RefundId}", response);
    }

    private static async Task<IResult> GetClientRefundDocumentAsync(
        Guid refundId,
        GetClientRefundDocumentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetClientRefundDocumentQuery(refundId), cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ClientRefundDocumentResponse(ToResponse(result.Value.Refund)));
    }

    private static async Task<IResult> ApplyClientCreditAsync(
        ApplyClientCreditRequest request,
        ApplyClientCreditHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ApplyClientCreditCommand(
                request.ClientId,
                request.InvoiceId,
                request.Reference,
                request.Amount,
                request.CurrencyCode,
                request.AppliedOn,
                request.Note),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new ApplyClientCreditResponse(
            result.Value.CreditApplicationId,
            result.Value.ClientId,
            result.Value.InvoiceId,
            result.Value.InvoiceNumber,
            result.Value.InvoiceStatus,
            result.Value.Reference,
            result.Value.Amount,
            result.Value.InvoiceBalanceBefore,
            result.Value.InvoiceBalanceAfter,
            result.Value.AvailableCreditBefore,
            result.Value.AvailableCreditAfter,
            result.Value.ClientBalanceBefore,
            result.Value.ClientBalanceAfter,
            result.Value.CurrencyCode,
            result.Value.AppliedOn,
            result.Value.CreditApplicationStatus);

        return Results.Created($"/api/v1/payments/client-credit-applications/{response.CreditApplicationId}", response);
    }

    private static GenerateInvoiceDraftResponse ToResponse(
        SafarSuite.ControlDesk.Application.Modules.Billing.GenerateInvoiceDraft.GenerateInvoiceDraftResult result)
    {
        return new GenerateInvoiceDraftResponse(
            result.InvoiceId,
            result.ClientId,
            result.ContractId,
            result.InvoiceNumber,
            result.IssueDate,
            result.DueDate,
            result.BillingDate,
            result.TotalAmount,
            result.BalanceDue,
            result.CurrencyCode,
            result.Status,
            result.Lines.Select(line => new GenerateInvoiceDraftLineResponse(
                line.ChargeCodeId,
                line.ProductModuleCode,
                line.LineType,
                line.Description,
                line.Amount,
                line.CurrencyCode)).ToArray());
    }

    private static RecordInvoicePaymentResponse ToResponse(RecordInvoicePaymentResult result)
    {
        return new RecordInvoicePaymentResponse(
            result.PaymentId,
            result.InvoiceId,
            result.InvoiceNumber,
            result.InvoiceStatus,
            result.PaymentStatus,
            result.Amount,
            result.BalanceDue,
            result.CurrencyCode,
            result.JournalEntryId,
            result.JournalEntryStatus,
            result.PostingDate,
            result.TotalDebit,
            result.TotalCredit,
            result.JournalLines.Select(line => new RecordInvoicePaymentJournalLineResponse(
                line.LedgerAccountId,
                line.Debit,
                line.Credit,
                line.Description)).ToArray());
    }

    private static ReverseInvoicePaymentResponse ToResponse(ReverseInvoicePaymentResult result)
    {
        return new ReverseInvoicePaymentResponse(
            result.PaymentId,
            result.InvoiceId,
            result.InvoiceNumber,
            result.InvoiceStatus,
            result.PaymentStatus,
            result.Amount,
            result.BalanceDue,
            result.CurrencyCode,
            result.ReversalJournalEntryId,
            result.ReversalJournalEntryStatus,
            result.ReversalDate,
            result.OriginalJournalEntryId,
            result.TotalDebit,
            result.TotalCredit,
            result.JournalLines.Select(line => new ReverseInvoicePaymentJournalLineResponse(
                line.LedgerAccountId,
                line.Debit,
                line.Credit,
                line.Description)).ToArray());
    }

    private static IssueClientRefundResponse ToResponse(IssueClientRefundResult result)
    {
        return new IssueClientRefundResponse(
            result.RefundId,
            result.ClientId,
            result.RefundStatus,
            result.Method,
            result.Reference,
            result.Amount,
            result.ClientBalanceBefore,
            result.ClientBalanceAfter,
            result.CurrencyCode,
            result.RefundedOn,
            result.JournalEntryId,
            result.JournalEntryStatus,
            result.PostingDate,
            result.TotalDebit,
            result.TotalCredit,
            result.JournalLines.Select(line => new IssueClientRefundJournalLineResponse(
                line.LedgerAccountId,
                line.Debit,
                line.Credit,
                line.Description)).ToArray());
    }
}
