namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureAccountCodeRange;

public sealed record ConfigureAccountCodeRangeCommand(
    string? CompanyCode,
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
