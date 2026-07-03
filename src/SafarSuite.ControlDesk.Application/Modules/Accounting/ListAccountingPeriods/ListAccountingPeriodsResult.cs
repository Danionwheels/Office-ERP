using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingPeriodCloseReadiness;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ListAccountingPeriods;

public sealed record ListAccountingPeriodsResult(
    string CompanyCode,
    IReadOnlyCollection<AccountingPeriodResult> Periods);

public sealed record AccountingPeriodResult(
    Guid AccountingPeriodId,
    string CompanyCode,
    string Name,
    DateOnly StartsOn,
    DateOnly EndsOn,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    DateTimeOffset? ReopenedAtUtc,
    AccountingPeriodCloseArtifactResult? CloseArtifact);

public sealed record AccountingPeriodCloseArtifactResult(
    DateTimeOffset GeneratedAtUtc,
    string GeneratedBy,
    int CheckCount,
    int BlockedCheckCount,
    int CurrencyCount,
    int PostedJournalCount,
    int DraftJournalCount,
    IReadOnlyCollection<AccountingPeriodCloseReadinessCheckResult> Checks,
    IReadOnlyCollection<AccountingPeriodCloseCurrencyResult> Currencies,
    IReadOnlyCollection<AccountingPeriodCloseJournalArtifactResult> CloseJournalEntries);
