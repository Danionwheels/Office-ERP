namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetTrialBalance;

public sealed record GetTrialBalanceResult(
    DateOnly? FromDate,
    DateOnly AsOfDate,
    string CurrencyCode,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal TotalPeriodDebit,
    decimal TotalPeriodCredit,
    decimal Difference,
    IReadOnlyCollection<TrialBalanceLineResult> Lines);

public sealed record TrialBalanceLineResult(
    Guid LedgerAccountId,
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    decimal OpeningBalance,
    decimal PeriodDebit,
    decimal PeriodCredit,
    decimal DebitBalance,
    decimal CreditBalance,
    decimal NetBalance,
    int ActivityCount);
