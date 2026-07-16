using SafarSuite.ControlDesk.Api.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetRevenueSummary;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Accounting;

namespace SafarSuite.ControlDesk.Api.Modules.Accounting;

public static class AccountingReportEndpoints
{
    public static IEndpointRouteBuilder MapAccountingReportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/accounting")
            .WithTags("Accounting Reports");

        group.MapGet("/revenue-summary", GetRevenueSummaryAsync);

        return endpoints;
    }

    private static async Task<IResult> GetRevenueSummaryAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        string? period,
        string? currencyCode,
        GetRevenueSummaryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetRevenueSummaryQuery(fromDate, toDate, period, currencyCode),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new RevenueSummaryResponse(
            result.Value.FromDate,
            result.Value.ToDate,
            result.Value.Period,
            result.Value.CurrencyCode,
            result.Value.TotalRevenue,
            result.Value.Periods.Select(item => new RevenueSummaryPeriodResponse(
                item.PeriodStart,
                item.PeriodEnd,
                item.Label,
                item.Debit,
                item.Credit,
                item.Revenue,
                item.ActivityCount)).ToArray()));
    }
}
