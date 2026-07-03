namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountReconciliation;

public sealed record GetLedgerAccountReconciliationResult(
    string CompanyCode,
    int AccountCount,
    int IssueCount,
    IReadOnlyCollection<LedgerAccountReconciliationItemResult> Items);

public sealed record LedgerAccountReconciliationItemResult(
    Guid LedgerAccountId,
    string Code,
    string DisplayCode,
    string Name,
    string Type,
    string NormalBalance,
    string Level,
    Guid? ParentAccountId,
    bool IsPostingAccount,
    string Status,
    string? RangeRole,
    string? RangeDisplayName,
    IReadOnlyCollection<LedgerAccountReconciliationIssueResult> Issues);

public sealed record LedgerAccountReconciliationIssueResult(
    string Severity,
    string Code,
    string Message);
