namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ListLedgerAccounts;

public sealed record ListLedgerAccountsResult(
    string CompanyCode,
    IReadOnlyCollection<LedgerAccountSummaryResult> Accounts);

public sealed record LedgerAccountSummaryResult(
    Guid LedgerAccountId,
    string Code,
    string DisplayCode,
    string Name,
    string Type,
    string NormalBalance,
    Guid? ParentAccountId,
    bool IsPostingAccount,
    string Status,
    DateTimeOffset CreatedAtUtc,
    string? RangeRole,
    string? RangeDisplayName);
