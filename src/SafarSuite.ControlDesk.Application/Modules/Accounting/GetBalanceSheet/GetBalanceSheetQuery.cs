namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetBalanceSheet;

public sealed record GetBalanceSheetQuery(
    DateOnly? AsOfDate,
    string? CurrencyCode);
