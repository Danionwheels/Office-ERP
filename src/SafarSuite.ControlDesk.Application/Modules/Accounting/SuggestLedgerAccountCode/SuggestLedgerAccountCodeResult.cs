namespace SafarSuite.ControlDesk.Application.Modules.Accounting.SuggestLedgerAccountCode;

public sealed record SuggestLedgerAccountCodeResult(
    string CompanyCode,
    string Role,
    string SuggestedCode,
    string DisplayCode,
    string Type,
    string NormalBalance,
    bool IsPostingAccount,
    string RangeStart,
    string RangeEnd,
    string? ParentCode,
    Guid? ParentAccountId,
    string? ParentAccountCode,
    string? ParentAccountName);
