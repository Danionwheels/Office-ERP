namespace SafarSuite.ControlDesk.Application.Modules.Accounting.PostOpeningBalanceImport;

public sealed record PostOpeningBalanceImportCommand(
    DateOnly EntryDate,
    string CurrencyCode,
    string? SourceReference,
    string? Memo,
    IReadOnlyCollection<PostOpeningBalanceImportLineCommand> Lines);

public sealed record PostOpeningBalanceImportLineCommand(
    string AccountCode,
    decimal Debit,
    decimal Credit,
    string? Description);
