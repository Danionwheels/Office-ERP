namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetRevenueSummary;

public sealed record GetRevenueSummaryResult(
    DateOnly FromDate,
    DateOnly ToDate,
    string Period,
    string CurrencyCode,
    decimal TotalRevenue,
    IReadOnlyCollection<RevenueSummaryPeriodResult> Periods);

public sealed record RevenueSummaryPeriodResult(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string Label,
    decimal Debit,
    decimal Credit,
    decimal Revenue,
    int ActivityCount);
