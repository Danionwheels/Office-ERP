namespace SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewChartOfAccountsImportText;

public sealed record PreviewChartOfAccountsImportTextResult(
    string CompanyCode,
    string Format,
    int ParsedLineCount,
    int IgnoredLineCount,
    bool CanImport,
    int InsertCount,
    int UpdateCount,
    int NoChangeCount,
    int RejectCount,
    int WarningCount,
    int IssueCount,
    IReadOnlyCollection<ChartOfAccountsImportParseIssueResult> ParseIssues,
    IReadOnlyCollection<ChartOfAccountsImportRowResult> Rows);

public sealed record ChartOfAccountsImportParseIssueResult(
    int LineNumber,
    string Column,
    string Message,
    string? RawValue);

public sealed record ChartOfAccountsImportRowResult(
    int LineNumber,
    string Action,
    string Code,
    string DisplayCode,
    string Name,
    string? ImportedLevel,
    string ResolvedLevel,
    string Type,
    string NormalBalance,
    bool IsPostingAccount,
    string? ParentCode,
    Guid? ParentAccountId,
    string? ParentSource,
    string? CurrencyCode,
    Guid? ExistingLedgerAccountId,
    string? ExistingStatus,
    string? RangeRole,
    string? RangeDisplayName,
    IReadOnlyCollection<ChartOfAccountsImportIssueResult> Issues);

public sealed record ChartOfAccountsImportIssueResult(
    string Severity,
    string Code,
    string Message);
