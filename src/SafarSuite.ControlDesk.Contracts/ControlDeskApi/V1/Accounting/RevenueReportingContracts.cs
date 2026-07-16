namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Accounting;

public sealed record RevenueSummaryResponse(
    DateOnly FromDate,
    DateOnly ToDate,
    string Period,
    string CurrencyCode,
    decimal TotalRevenue,
    IReadOnlyCollection<RevenueSummaryPeriodResponse> Periods);

public sealed record RevenueSummaryPeriodResponse(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string Label,
    decimal Debit,
    decimal Credit,
    decimal Revenue,
    int ActivityCount);
