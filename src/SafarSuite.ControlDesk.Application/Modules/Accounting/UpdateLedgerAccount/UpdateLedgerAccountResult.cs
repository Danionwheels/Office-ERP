namespace SafarSuite.ControlDesk.Application.Modules.Accounting.UpdateLedgerAccount;

public sealed record UpdateLedgerAccountResult(
    Guid LedgerAccountId,
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    Guid? ParentAccountId,
    bool IsPostingAccount,
    string Status,
    DateTimeOffset CreatedAtUtc);
