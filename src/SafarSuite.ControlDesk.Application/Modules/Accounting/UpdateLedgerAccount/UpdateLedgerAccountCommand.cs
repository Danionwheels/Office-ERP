namespace SafarSuite.ControlDesk.Application.Modules.Accounting.UpdateLedgerAccount;

public sealed record UpdateLedgerAccountCommand(
    Guid LedgerAccountId,
    string Name,
    bool IsPostingAccount,
    string Status);
