namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetBalanceSheet;

public sealed record GetBalanceSheetResult(
    DateOnly AsOfDate,
    string CurrencyCode,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal TotalEquity,
    decimal TotalLiabilitiesAndEquity,
    decimal Difference,
    IReadOnlyCollection<BalanceSheetSectionResult> Sections);

public sealed record BalanceSheetSectionResult(
    string Type,
    string Title,
    decimal Total,
    IReadOnlyCollection<BalanceSheetLineResult> Lines);

public sealed record BalanceSheetLineResult(
    Guid? LedgerAccountId,
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    decimal Debit,
    decimal Credit,
    decimal Amount,
    int ActivityCount,
    bool IsSystemLine);
