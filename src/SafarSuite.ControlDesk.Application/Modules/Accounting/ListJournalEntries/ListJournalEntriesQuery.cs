namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ListJournalEntries;

public sealed record ListJournalEntriesQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    string? SourceType = null,
    string? Search = null,
    int Take = 50,
    string? Cursor = null);
