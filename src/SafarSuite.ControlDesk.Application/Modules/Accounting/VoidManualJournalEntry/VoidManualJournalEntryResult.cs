namespace SafarSuite.ControlDesk.Application.Modules.Accounting.VoidManualJournalEntry;

public sealed record VoidManualJournalEntryResult(
    Guid OriginalJournalEntryId,
    Guid ReversalJournalEntryId,
    string OriginalJournalEntryStatus,
    string ReversalJournalEntryStatus,
    DateOnly VoidDate,
    decimal TotalDebit,
    decimal TotalCredit,
    string CurrencyCode,
    IReadOnlyCollection<VoidManualJournalEntryLineResult> Lines);

public sealed record VoidManualJournalEntryLineResult(
    Guid LedgerAccountId,
    decimal Debit,
    decimal Credit,
    string? Description);
