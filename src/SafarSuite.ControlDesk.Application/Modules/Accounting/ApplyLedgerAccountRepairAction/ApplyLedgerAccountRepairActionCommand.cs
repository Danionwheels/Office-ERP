namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ApplyLedgerAccountRepairAction;

public sealed record ApplyLedgerAccountRepairActionCommand(
    Guid LedgerAccountId,
    string? CompanyCode,
    string IssueCode,
    string ActionCode,
    bool Confirmed);
