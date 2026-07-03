namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ListAccountCodeRanges;

public sealed record ListAccountCodeRangesResult(
    string CompanyCode,
    IReadOnlyCollection<AccountCodeRangeResult> Ranges);

public sealed record AccountCodeRangeResult(
    Guid AccountCodeRangeId,
    string CompanyCode,
    string Role,
    string DisplayName,
    string SearchPrefix,
    string RangeStart,
    string RangeEnd,
    int CodeLength,
    string AccountType,
    string NormalBalance,
    bool IsPostingAccount,
    string? ParentCode,
    bool IsActive);
