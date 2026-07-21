namespace SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewOpeningBalanceImportText;

public sealed record PreviewOpeningBalanceImportTextCommand(
    DateOnly EntryDate,
    string CurrencyCode,
    string? SourceReference,
    string? Memo,
    DateOnly ProfileFromDate,
    DateOnly ProfileToDate,
    string ProfileStatus,
    bool TransactionsAllowed,
    Guid? ProfitAndLossCarryForwardAccountId,
    string ImportText,
    string? Delimiter);
