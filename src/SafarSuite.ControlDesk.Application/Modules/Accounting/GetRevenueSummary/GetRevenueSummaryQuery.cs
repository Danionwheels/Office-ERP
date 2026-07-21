namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetRevenueSummary;

public sealed record GetRevenueSummaryQuery(
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? Period,
    string? CurrencyCode);
