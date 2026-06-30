namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ListJournalEntries;

public sealed record ListJournalEntriesQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    string? SourceType = null);
