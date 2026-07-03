namespace SafarSuite.ControlDesk.Application.Modules.Accounting.CreateLedgerAccount;

public sealed record CreateLedgerAccountCommand(
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    Guid? ParentAccountId,
    bool IsPostingAccount,
    string? Level = null);
