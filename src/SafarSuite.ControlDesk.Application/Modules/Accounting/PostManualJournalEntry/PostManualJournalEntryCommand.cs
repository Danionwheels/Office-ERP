namespace SafarSuite.ControlDesk.Application.Modules.Accounting.PostManualJournalEntry;

public sealed record PostManualJournalEntryCommand(
    DateOnly EntryDate,
    string CurrencyCode,
    string? SourceReference,
    string? Memo,
    IReadOnlyCollection<PostManualJournalEntryLineCommand> Lines);

public sealed record PostManualJournalEntryLineCommand(
    Guid LedgerAccountId,
    decimal Debit,
    decimal Credit,
    string? Description);
