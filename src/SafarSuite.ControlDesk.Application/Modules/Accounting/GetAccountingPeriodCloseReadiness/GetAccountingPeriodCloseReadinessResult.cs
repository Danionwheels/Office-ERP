using SafarSuite.ControlDesk.Application.Modules.Accounting.ListAccountingPeriods;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingPeriodCloseReadiness;

public sealed record GetAccountingPeriodCloseReadinessResult(
    AccountingPeriodResult Period,
    bool CanClose,
    IReadOnlyCollection<AccountingPeriodCloseReadinessCheckResult> Checks,
    IReadOnlyCollection<AccountingPeriodCloseCurrencyResult> Currencies);

public sealed record AccountingPeriodCloseReadinessCheckResult(
    string Code,
    string Status,
    string Message,
    string? Target);

public sealed record AccountingPeriodCloseCurrencyResult(
    string CurrencyCode,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal Difference,
    int PostedJournalCount,
    int DraftJournalCount);
