namespace SafarSuite.ControlDesk.Application.Modules.Accounting.VoidManualJournalEntry;

public sealed record VoidManualJournalEntryCommand(
    Guid JournalEntryId,
    DateOnly VoidDate,
    string Reason);
