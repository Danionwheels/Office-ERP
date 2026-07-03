namespace SafarSuite.ControlDesk.Application.Modules.Accounting.PostManualJournalEntry;

public sealed record PostManualJournalEntryResult(
    Guid JournalEntryId,
    DateOnly EntryDate,
    string CurrencyCode,
    string SourceType,
    string? SourceReference,
    string? Memo,
    string Status,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyCollection<PostManualJournalEntryLineResult> Lines);

public sealed record PostManualJournalEntryLineResult(
    Guid LedgerAccountId,
    decimal Debit,
    decimal Credit,
    string? Description);
