namespace SafarSuite.ControlDesk.Application.Modules.Accounting.CreateLedgerAccount;

public sealed record CreateLedgerAccountResult(
    Guid LedgerAccountId,
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    Guid? ParentAccountId,
    bool IsPostingAccount,
    string Status);
