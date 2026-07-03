using SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingPeriodCloseReadiness;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.Common;

public sealed record AccountingPeriodCloseArtifactSnapshot(
    int Version,
    DateTimeOffset GeneratedAtUtc,
    string GeneratedBy,
    AccountingPeriodCloseArtifactPeriodSnapshot Period,
    bool CanClose,
    IReadOnlyCollection<AccountingPeriodCloseReadinessCheckResult> Checks,
    IReadOnlyCollection<AccountingPeriodCloseCurrencyResult> Currencies)
{
    public IReadOnlyCollection<AccountingPeriodCloseJournalArtifactResult> CloseJournalEntries { get; init; } =
        Array.Empty<AccountingPeriodCloseJournalArtifactResult>();
}

public sealed record AccountingPeriodCloseArtifactPeriodSnapshot(
    Guid AccountingPeriodId,
    string CompanyCode,
    string Name,
    DateOnly StartsOn,
    DateOnly EndsOn,
    string Status);

public sealed record AccountingPeriodCloseJournalArtifactResult(
    Guid JournalEntryId,
    string SourceReference,
    string Memo,
    DateOnly EntryDate,
    string CurrencyCode,
    decimal TotalDebit,
    decimal TotalCredit);
