namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetProfitAndLossStatement;

public sealed record GetProfitAndLossStatementResult(
    DateOnly? FromDate,
    DateOnly ToDate,
    string CurrencyCode,
    decimal TotalRevenue,
    decimal TotalExpense,
    decimal NetIncome,
    IReadOnlyCollection<ProfitAndLossStatementSectionResult> Sections);

public sealed record ProfitAndLossStatementSectionResult(
    string Type,
    string Title,
    decimal Total,
    IReadOnlyCollection<ProfitAndLossStatementLineResult> Lines);

public sealed record ProfitAndLossStatementLineResult(
    Guid LedgerAccountId,
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    decimal Debit,
    decimal Credit,
    decimal Amount,
    int ActivityCount);
