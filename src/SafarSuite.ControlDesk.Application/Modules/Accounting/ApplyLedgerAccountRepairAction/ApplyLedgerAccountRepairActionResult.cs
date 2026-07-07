using SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountRepairPlan;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ApplyLedgerAccountRepairAction;

public sealed record ApplyLedgerAccountRepairActionResult(
    Guid LedgerAccountId,
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    string Level,
    Guid? ParentAccountId,
    bool IsPostingAccount,
    string Status,
    DateTimeOffset CreatedAtUtc,
    LedgerAccountRepairActionResult AppliedAction);
