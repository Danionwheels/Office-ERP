namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ListJournalEntries;

public sealed record ListJournalEntriesResult(
    IReadOnlyCollection<JournalEntryRegisterItemResult> Entries,
    int PageSize,
    bool HasMore,
    string? NextCursor,
    long FilteredCount);

public sealed record JournalEntryRegisterItemResult(
    Guid JournalEntryId,
    DateOnly EntryDate,
    string CurrencyCode,
    string SourceType,
    string? SourceReference,
    string? Memo,
    string Status,
    decimal TotalDebit,
    decimal TotalCredit,
    int LineCount);

public sealed record JournalEntrySummaryResult(
    Guid JournalEntryId,
    DateOnly EntryDate,
    string CurrencyCode,
    string SourceType,
    string? SourceReference,
    string? Memo,
    string Status,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyCollection<JournalEntryLineResult> Lines);

public sealed record JournalEntryLineResult(
    Guid LedgerAccountId,
    decimal Debit,
    decimal Credit,
    string? Description);
