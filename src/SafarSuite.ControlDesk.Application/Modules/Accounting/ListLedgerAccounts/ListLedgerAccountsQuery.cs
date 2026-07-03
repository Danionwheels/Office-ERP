namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ListLedgerAccounts;

public sealed record ListLedgerAccountsQuery(
    string? CompanyCode = null,
    string? Search = null,
    string? Type = null,
    string? Status = null,
    bool? IsPostingAccount = null,
    string? Role = null);
