namespace SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewOpeningBalanceImport;

public sealed record PreviewOpeningBalanceImportResult(
    DateOnly EntryDate,
    string CurrencyCode,
    string SourceReference,
    string? Memo,
    bool CanPost,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal Difference,
    int ImportedLineCount,
    int ValidLineCount,
    int InvalidLineCount,
    IReadOnlyCollection<string> Blockers,
    IReadOnlyCollection<PreviewOpeningBalanceImportLineResult> Lines);

public sealed record PreviewOpeningBalanceImportLineResult(
    int LineNumber,
    string AccountCode,
    Guid? LedgerAccountId,
    string? LedgerAccountName,
    string? AccountType,
    string? NormalBalance,
    decimal Debit,
    decimal Credit,
    string? Description,
    bool IsValid,
    IReadOnlyCollection<string> Issues);
