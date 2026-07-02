using SafarSuite.ControlDesk.Api.Common;
using SafarSuite.ControlDesk.Application.Modules.Billing.CreateChargeCode;
using SafarSuite.ControlDesk.Application.Modules.Billing.CreateClientChargeRule;
using SafarSuite.ControlDesk.Application.Modules.Billing.GenerateInvoiceDraft;
using SafarSuite.ControlDesk.Application.Modules.Billing.IssueCreditNote;
using SafarSuite.ControlDesk.Application.Modules.Billing.IssueInvoice;
using SafarSuite.ControlDesk.Application.Modules.Billing.ListChargeCodes;
using SafarSuite.ControlDesk.Application.Modules.Billing.ListClientChargeRules;
using SafarSuite.ControlDesk.Application.Modules.Billing.VoidInvoice;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Billing;

namespace SafarSuite.ControlDesk.Api.Modules.Billing;

public static class BillingEndpoints
{
    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/billing")
            .WithTags("Billing");

        group.MapPost("/charge-codes", CreateChargeCodeAsync);
        group.MapGet("/charge-codes", ListChargeCodesAsync);
        group.MapPost("/client-charge-rules", CreateClientChargeRuleAsync);
        group.MapGet("/clients/{clientId:guid}/client-charge-rules", ListClientChargeRulesAsync);
        group.MapPost("/invoice-drafts", GenerateInvoiceDraftAsync);
        group.MapPost("/invoices/{invoiceId:guid}/issue", IssueInvoiceAsync);
        group.MapPost("/invoices/{invoiceId:guid}/void", VoidInvoiceAsync);
        group.MapPost("/invoices/{invoiceId:guid}/credit-notes", IssueCreditNoteAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateChargeCodeAsync(
        CreateChargeCodeRequest request,
        CreateChargeCodeHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateChargeCodeCommand(
            request.Code,
            request.Name,
            request.Description,
            request.DefaultUnitPriceAmount,
            request.CurrencyCode,
            request.RevenueAccountId,
            request.TaxAccountId);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new CreateChargeCodeResponse(
            result.Value.ChargeCodeId,
            result.Value.Code,
            result.Value.Name,
            result.Value.DefaultUnitPriceAmount,
            result.Value.CurrencyCode,
            result.Value.RevenueAccountId,
            result.Value.TaxAccountId,
            result.Value.Status);

        return Results.Created($"/api/v1/billing/charge-codes/{response.ChargeCodeId}", response);
    }

    private static async Task<IResult> ListChargeCodesAsync(
        ListChargeCodesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new ListChargeCodesResponse(
            result.Value.ChargeCodes.Select(chargeCode => new ChargeCodeLookupResponse(
                chargeCode.ChargeCodeId,
                chargeCode.Code,
                chargeCode.Name,
                chargeCode.DefaultUnitPriceAmount,
                chargeCode.CurrencyCode,
                chargeCode.RevenueAccountId,
                chargeCode.TaxAccountId,
                chargeCode.Status)).ToArray());

        return Results.Ok(response);
    }

    private static async Task<IResult> CreateClientChargeRuleAsync(
        CreateClientChargeRuleRequest request,
        CreateClientChargeRuleHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateClientChargeRuleCommand(
            request.ClientId,
            request.ContractId,
            request.ChargeCodeId,
            request.ProductModuleCode,
            request.DescriptionOverride,
            request.UnitPriceAmount,
            request.CurrencyCode,
            request.Quantity,
            request.TaxPercent,
            request.BillingCycle,
            request.BillingDayOfMonth,
            request.EffectiveStartsOn,
            request.EffectiveEndsOn);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new CreateClientChargeRuleResponse(
            result.Value.ClientChargeRuleId,
            result.Value.ClientId,
            result.Value.ContractId,
            result.Value.ChargeCodeId,
            result.Value.ProductModuleCode,
            result.Value.UnitPriceAmount,
            result.Value.CurrencyCode,
            result.Value.Quantity,
            result.Value.TaxPercent,
            result.Value.TaxAmount,
            result.Value.LineAmount,
            result.Value.TotalLineAmount,
            result.Value.BillingCycle,
            result.Value.BillingDayOfMonth,
            result.Value.EffectiveStartsOn,
            result.Value.EffectiveEndsOn,
            result.Value.Status);

        return Results.Created($"/api/v1/billing/client-charge-rules/{response.ClientChargeRuleId}", response);
    }

    private static async Task<IResult> ListClientChargeRulesAsync(
        Guid clientId,
        Guid? contractId,
        DateOnly? effectiveOn,
        ListClientChargeRulesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListClientChargeRulesQuery(clientId, contractId, effectiveOn),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new ListClientChargeRulesResponse(
            result.Value.EffectiveOn,
            result.Value.ChargeRules.Select(rule => new ClientChargeRuleLookupResponse(
                rule.ClientChargeRuleId,
                rule.ClientId,
                rule.ContractId,
                rule.ChargeCodeId,
                rule.ProductModuleCode,
                rule.UnitPriceAmount,
                rule.CurrencyCode,
                rule.Quantity,
                rule.TaxPercent,
                rule.TaxAmount,
                rule.LineAmount,
                rule.TotalLineAmount,
                rule.BillingCycle,
                rule.BillingDayOfMonth,
                rule.EffectiveStartsOn,
                rule.EffectiveEndsOn,
                rule.Status)).ToArray());

        return Results.Ok(response);
    }

    private static async Task<IResult> GenerateInvoiceDraftAsync(
        GenerateInvoiceDraftRequest request,
        GenerateInvoiceDraftHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new GenerateInvoiceDraftCommand(
            request.ClientId,
            request.ContractId,
            request.InvoiceNumber,
            request.IssueDate,
            request.DueDate,
            request.BillingDate,
            request.CurrencyCode);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new GenerateInvoiceDraftResponse(
            result.Value.InvoiceId,
            result.Value.ClientId,
            result.Value.ContractId,
            result.Value.InvoiceNumber,
            result.Value.IssueDate,
            result.Value.DueDate,
            result.Value.BillingDate,
            result.Value.TotalAmount,
            result.Value.BalanceDue,
            result.Value.CurrencyCode,
            result.Value.Status,
            result.Value.Lines.Select(line => new GenerateInvoiceDraftLineResponse(
                line.ChargeCodeId,
                line.ProductModuleCode,
                line.LineType,
                line.Description,
                line.Amount,
                line.CurrencyCode)).ToArray());

        return Results.Created($"/api/v1/billing/invoices/{response.InvoiceId}", response);
    }

    private static async Task<IResult> IssueInvoiceAsync(
        Guid invoiceId,
        IssueInvoiceRequest request,
        IssueInvoiceHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new IssueInvoiceCommand(
            invoiceId,
            request.AccountsReceivableAccountId,
            request.PostingDate);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new IssueInvoiceResponse(
            result.Value.InvoiceId,
            result.Value.InvoiceNumber,
            result.Value.InvoiceStatus,
            result.Value.JournalEntryId,
            result.Value.JournalEntryStatus,
            result.Value.PostingDate,
            result.Value.TotalDebit,
            result.Value.TotalCredit,
            result.Value.CurrencyCode,
            result.Value.JournalLines.Select(line => new IssueInvoiceJournalLineResponse(
                line.LedgerAccountId,
                line.Debit,
                line.Credit,
                line.Description)).ToArray());

        return Results.Ok(response);
    }

    private static async Task<IResult> VoidInvoiceAsync(
        Guid invoiceId,
        VoidInvoiceRequest request,
        VoidInvoiceHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new VoidInvoiceCommand(
            invoiceId,
            request.VoidDate,
            request.Reason);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new VoidInvoiceResponse(
            result.Value.InvoiceId,
            result.Value.InvoiceNumber,
            result.Value.InvoiceStatus,
            result.Value.OriginalJournalEntryId,
            result.Value.ReversalJournalEntryId,
            result.Value.ReversalJournalEntryStatus,
            result.Value.VoidDate,
            result.Value.TotalDebit,
            result.Value.TotalCredit,
            result.Value.CurrencyCode,
            result.Value.JournalLines.Select(line => new VoidInvoiceJournalLineResponse(
                line.LedgerAccountId,
                line.Debit,
                line.Credit,
                line.Description)).ToArray());

        return Results.Ok(response);
    }

    private static async Task<IResult> IssueCreditNoteAsync(
        Guid invoiceId,
        IssueCreditNoteRequest request,
        IssueCreditNoteHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new IssueCreditNoteCommand(
            invoiceId,
            request.CreditNoteNumber,
            request.CreditDate,
            request.Reason);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new IssueCreditNoteResponse(
            result.Value.CreditNoteId,
            result.Value.InvoiceId,
            result.Value.CreditNoteNumber,
            result.Value.InvoiceNumber,
            result.Value.CreditNoteStatus,
            result.Value.CreditDate,
            result.Value.Amount,
            result.Value.CurrencyCode,
            result.Value.JournalEntryId,
            result.Value.JournalEntryStatus,
            result.Value.TotalDebit,
            result.Value.TotalCredit,
            result.Value.JournalLines.Select(line => new IssueCreditNoteJournalLineResponse(
                line.LedgerAccountId,
                line.Debit,
                line.Credit,
                line.Description)).ToArray());

        return Results.Created($"/api/v1/billing/credit-notes/{response.CreditNoteId}", response);
    }
}
