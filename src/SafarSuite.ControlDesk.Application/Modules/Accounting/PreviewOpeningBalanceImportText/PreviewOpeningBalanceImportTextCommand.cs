namespace SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewOpeningBalanceImportText;

public sealed record PreviewOpeningBalanceImportTextCommand(
    DateOnly EntryDate,
    string CurrencyCode,
    string? SourceReference,
    string? Memo,
    string ImportText,
    string? Delimiter);
