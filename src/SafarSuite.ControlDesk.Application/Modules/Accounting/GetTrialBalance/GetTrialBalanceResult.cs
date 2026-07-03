namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetTrialBalance;

public sealed record GetTrialBalanceResult(
    DateOnly AsOfDate,
    string CurrencyCode,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal Difference,
    IReadOnlyCollection<TrialBalanceLineResult> Lines);

public sealed record TrialBalanceLineResult(
    Guid LedgerAccountId,
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    decimal DebitBalance,
    decimal CreditBalance,
    decimal NetBalance,
    int ActivityCount);
