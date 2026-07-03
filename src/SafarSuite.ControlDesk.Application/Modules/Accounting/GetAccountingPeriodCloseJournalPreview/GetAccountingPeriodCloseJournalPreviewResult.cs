using SafarSuite.ControlDesk.Application.Modules.Accounting.ListAccountingPeriods;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingPeriodCloseJournalPreview;

public sealed record GetAccountingPeriodCloseJournalPreviewResult(
    AccountingPeriodResult Period,
    string BaseCurrencyCode,
    bool CanGenerate,
    decimal NetIncome,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyCollection<string> Blockers,
    IReadOnlyCollection<AccountingCloseJournalPreviewEntryResult> Entries);

public sealed record AccountingCloseJournalPreviewEntryResult(
    string SourceReference,
    string Memo,
    DateOnly EntryDate,
    string CurrencyCode,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyCollection<AccountingCloseJournalPreviewLineResult> Lines);

public sealed record AccountingCloseJournalPreviewLineResult(
    Guid LedgerAccountId,
    string Code,
    string Name,
    string Type,
    decimal Debit,
    decimal Credit,
    string Description);
