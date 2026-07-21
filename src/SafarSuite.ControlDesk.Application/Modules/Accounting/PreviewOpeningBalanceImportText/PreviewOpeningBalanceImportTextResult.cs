using SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewOpeningBalanceImport;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewOpeningBalanceImportText;

public sealed record PreviewOpeningBalanceImportTextResult(
    string Format,
    int ParsedLineCount,
    int IgnoredLineCount,
    IReadOnlyCollection<OpeningBalanceImportTextParseIssueResult> ParseIssues,
    PreviewOpeningBalanceImportResult Preview);

public sealed record OpeningBalanceImportTextParseIssueResult(
    int LineNumber,
    string Column,
    string Message,
    string? RawValue);
