namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountRepairPlan;

public sealed record GetLedgerAccountRepairPlanResult(
    string CompanyCode,
    int AccountCount,
    int IssueCount,
    int ActionCount,
    IReadOnlyCollection<LedgerAccountRepairPlanItemResult> Items);

public sealed record LedgerAccountRepairPlanItemResult(
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
    IReadOnlyCollection<LedgerAccountRepairActionResult> Actions);

public sealed record LedgerAccountRepairActionResult(
    string IssueCode,
    string Severity,
    string ActionCode,
    string Title,
    string Description,
    string RepairMode,
    bool IsAutomatable,
    string? CurrentValue,
    string? SuggestedValue,
    IReadOnlyCollection<string> Notes);
