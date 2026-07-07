namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountCodeRangeValidation;

public sealed record GetAccountCodeRangeValidationResult(
    string CompanyCode,
    int RangeCount,
    int ActiveRangeCount,
    bool IsValid,
    int ErrorCount,
    int WarningCount,
    int IssueCount,
    IReadOnlyCollection<AccountCodeRangeValidationIssueResult> Issues);

public sealed record AccountCodeRangeValidationIssueResult(
    string Severity,
    string Code,
    string Message,
    string? RangeRole,
    string? RelatedRangeRole,
    string? RangeStart,
    string? RangeEnd);
