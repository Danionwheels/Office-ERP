namespace SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewJournalVoucherNumber;

public sealed record PreviewJournalVoucherNumberQuery(
    string SourceType,
    DateOnly EntryDate);
