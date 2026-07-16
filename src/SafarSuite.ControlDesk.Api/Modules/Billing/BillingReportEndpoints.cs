using SafarSuite.ControlDesk.Api.Common;
using SafarSuite.ControlDesk.Application.Modules.Billing.GetAccountsReceivableAging;
using SafarSuite.ControlDesk.Application.Modules.Billing.ListOutstandingInvoices;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Billing;

namespace SafarSuite.ControlDesk.Api.Modules.Billing;

public static class BillingReportEndpoints
{
    public static IEndpointRouteBuilder MapBillingReportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/billing/reports")
            .WithTags("Billing Reports");

        group.MapGet("/accounts-receivable-aging", GetAccountsReceivableAgingAsync);
        group.MapGet("/outstanding-invoices", ListOutstandingInvoicesAsync);

        return endpoints;
    }

    private static async Task<IResult> GetAccountsReceivableAgingAsync(
        DateOnly? asOfDate,
        string? currencyCode,
        GetAccountsReceivableAgingHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetAccountsReceivableAgingQuery(asOfDate, currencyCode),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new AccountsReceivableAgingResponse(
            result.Value.AsOfDate,
            result.Value.Currencies.Select(currency => new AccountsReceivableAgingCurrencyResponse(
                currency.CurrencyCode,
                currency.CurrentAmount,
                currency.Days1To30Amount,
                currency.Days31To60Amount,
                currency.Days61To90Amount,
                currency.DaysOver90Amount,
                currency.TotalOutstanding,
                currency.InvoiceCount,
                currency.ClientCount)).ToArray(),
            result.Value.Clients.Select(client => new AccountsReceivableAgingClientResponse(
                client.ClientId,
                client.ClientCode,
                client.ClientName,
                client.CurrencyCode,
                client.CurrentAmount,
                client.Days1To30Amount,
                client.Days31To60Amount,
                client.Days61To90Amount,
                client.DaysOver90Amount,
                client.TotalOutstanding,
                client.InvoiceCount)).ToArray()));
    }

    private static async Task<IResult> ListOutstandingInvoicesAsync(
        Guid? clientId,
        DateOnly? fromDate,
        DateOnly? toDate,
        decimal? minAmount,
        decimal? maxAmount,
        string? status,
        string? currencyCode,
        int? take,
        string? cursor,
        ListOutstandingInvoicesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListOutstandingInvoicesQuery(
                clientId,
                fromDate,
                toDate,
                minAmount,
                maxAmount,
                status,
                currencyCode,
                take ?? 25,
                cursor),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new OutstandingInvoicePageResponse(
            result.Value.Invoices.Select(invoice => new OutstandingInvoiceResponse(
                invoice.InvoiceId,
                invoice.ClientId,
                invoice.ClientCode,
                invoice.ClientName,
                invoice.InvoiceNumber,
                invoice.IssueDate,
                invoice.DueDate,
                invoice.Status,
                invoice.TotalAmount,
                invoice.AmountPaid,
                invoice.BalanceDue,
                invoice.CurrencyCode,
                invoice.DaysOverdue,
                invoice.AgingBucket,
                invoice.JournalEntryId)).ToArray(),
            result.Value.PageSize,
            result.Value.HasMore,
            result.Value.NextCursor,
            result.Value.FilteredCount));
    }
}
