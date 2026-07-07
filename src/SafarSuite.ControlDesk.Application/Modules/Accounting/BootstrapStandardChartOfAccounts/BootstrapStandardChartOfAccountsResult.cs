namespace SafarSuite.ControlDesk.Application.Modules.Accounting.BootstrapStandardChartOfAccounts;

public sealed record BootstrapStandardChartOfAccountsResult(
    string CompanyCode,
    int CreatedCount,
    int ReusedCount,
    IReadOnlyCollection<BootstrapStandardChartOfAccountsItemResult> Accounts);

public sealed record BootstrapStandardChartOfAccountsItemResult(
    Guid LedgerAccountId,
    string Role,
    string Action,
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    string Level,
    Guid? ParentAccountId,
    bool IsPostingAccount,
    string Status);
