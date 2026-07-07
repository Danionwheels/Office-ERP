namespace SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewOpeningBalanceImport;

public sealed record PreviewOpeningBalanceImportCommand(
    DateOnly EntryDate,
    string CurrencyCode,
    string? SourceReference,
    string? Memo,
    DateOnly ProfileFromDate,
    DateOnly ProfileToDate,
    string ProfileStatus,
    bool TransactionsAllowed,
    Guid? ProfitAndLossCarryForwardAccountId,
    IReadOnlyCollection<PreviewOpeningBalanceImportLineCommand> Lines);

public sealed record PreviewOpeningBalanceImportLineCommand(
    string AccountCode,
    decimal Debit,
    decimal Credit,
    string? Description);
