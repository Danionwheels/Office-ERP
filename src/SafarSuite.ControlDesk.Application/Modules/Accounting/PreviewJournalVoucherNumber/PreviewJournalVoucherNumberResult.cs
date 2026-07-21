namespace SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewJournalVoucherNumber;

public sealed record PreviewJournalVoucherNumberResult(
    string SourceType,
    DateOnly EntryDate,
    string Prefix,
    int SequenceYear,
    int NextSequence,
    int NumberPaddingWidth,
    string Reference);
