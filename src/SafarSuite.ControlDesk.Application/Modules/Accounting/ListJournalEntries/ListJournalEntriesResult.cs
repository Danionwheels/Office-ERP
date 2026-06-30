namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ListJournalEntries;

public sealed record ListJournalEntriesResult(
    IReadOnlyCollection<JournalEntrySummaryResult> Entries);

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
