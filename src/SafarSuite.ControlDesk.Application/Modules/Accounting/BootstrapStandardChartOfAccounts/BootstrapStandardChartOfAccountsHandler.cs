using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.CreateLedgerAccount;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.BootstrapStandardChartOfAccounts;

public sealed class BootstrapStandardChartOfAccountsHandler
{
    private static readonly IReadOnlyCollection<StandardLedgerAccountDefinition> StandardAccounts =
    [
        new("AssetHeader", "10000", "Assets", "Asset", "Debit", "Header", false),
        new("AssetTotal", "19000", "Total assets", "Asset", "Debit", "Total", false, "AssetHeader"),
        new("CashBankControl", "14100", "Cash and bank control", "Asset", "Debit", "Control", false, "AssetTotal"),
        new("CashBank", "14110", "Cash on hand and bank", "Asset", "Debit", "Detail", true, "CashBankControl"),
        new("Refund", "14200", "Client refund clearing", "Asset", "Debit", "Detail", true),
        new("ReceivableControl", "15100", "Accounts receivable control", "Asset", "Debit", "Control", false, "AssetTotal"),
        new("ClientReceivable", "151000001", "Client receivables", "Asset", "Debit", "Subsidiary", true, "ReceivableControl"),
        new("EquityHeader", "20000", "Equity", "Equity", "Credit", "Header", false),
        new("EquityTotal", "29000", "Total equity", "Equity", "Credit", "Total", false, "EquityHeader"),
        new("RetainedEarnings", "21000", "Retained earnings", "Equity", "Credit", "Detail", true),
        new("IncomeSummary", "23000", "Income summary", "Equity", "Credit", "Detail", true),
        new("LiabilityHeader", "30000", "Liabilities", "Liability", "Credit", "Header", false),
        new("LiabilityTotal", "39000", "Total liabilities", "Liability", "Credit", "Total", false, "LiabilityHeader"),
        new("TaxPayable", "32100", "Tax payable", "Liability", "Credit", "Detail", true),
        new("RevenueHeader", "40000", "Revenue", "Revenue", "Credit", "Header", false),
        new("RevenueTotal", "59000", "Total revenue", "Revenue", "Credit", "Total", false, "RevenueHeader"),
        new("SubscriptionRevenue", "41000", "Subscription revenue", "Revenue", "Credit", "Detail", true),
        new("Discount", "52000", "Client discounts", "Revenue", "Debit", "Detail", true),
        new("ExpenseHeader", "60000", "Expenses", "Expense", "Debit", "Header", false),
        new("ExpenseTotal", "99000", "Total expenses", "Expense", "Debit", "Total", false, "ExpenseHeader"),
        new("RoundingAdjustment", "61000", "Rounding adjustment", "Expense", "Debit", "Detail", true)
    ];

    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly AccountingSetupDefaults _defaults;
    private readonly CreateLedgerAccountHandler _createLedgerAccount;

    public BootstrapStandardChartOfAccountsHandler(
        ILedgerAccountRepository ledgerAccounts,
        AccountingSetupDefaults defaults,
        CreateLedgerAccountHandler createLedgerAccount)
    {
        _ledgerAccounts = ledgerAccounts;
        _defaults = defaults;
        _createLedgerAccount = createLedgerAccount;
    }

    public async Task<Result<BootstrapStandardChartOfAccountsResult>> HandleAsync(
        BootstrapStandardChartOfAccountsCommand command,
        CancellationToken cancellationToken = default)
    {
        var companyError = AccountingSetupDefaults.ValidateSingleCompanyCode(
            command.CompanyCode,
            nameof(command.CompanyCode));

        if (companyError is not null)
        {
            return Result<BootstrapStandardChartOfAccountsResult>.Failure(companyError);
        }

        var companyCode = AccountingSetupDefaults.NormalizeCompanyCode(command.CompanyCode);
        await _defaults.EnsureSeededAsync(companyCode, cancellationToken);

        var items = new List<BootstrapStandardChartOfAccountsItemResult>();
        var accountsByRole = new Dictionary<string, LedgerAccount>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in StandardAccounts)
        {
            var existing = await _ledgerAccounts.GetByCodeAsync(
                LedgerAccountCode.Create(definition.Code),
                cancellationToken);

            if (existing is not null)
            {
                accountsByRole[definition.Role] = existing;
                items.Add(ToResultItem(definition.Role, "Reused", existing));
                continue;
            }

            var parentAccountId = ResolveParentAccountId(definition, accountsByRole);
            var created = await _createLedgerAccount.HandleAsync(
                new CreateLedgerAccountCommand(
                    definition.Code,
                    definition.Name,
                    definition.Type,
                    definition.NormalBalance,
                    parentAccountId,
                    definition.IsPostingAccount,
                    definition.Level),
                cancellationToken);

            if (created.IsFailure)
            {
                return Result<BootstrapStandardChartOfAccountsResult>.Failure(created.Errors);
            }

            var account = await _ledgerAccounts.GetByIdAsync(
                LedgerAccountId.Create(created.Value.LedgerAccountId),
                cancellationToken);

            if (account is null)
            {
                return Result<BootstrapStandardChartOfAccountsResult>.Failure(
                    ApplicationError.NotFound(
                        definition.Role,
                        $"Standard ledger account {definition.Code} was not found after creation."));
            }

            accountsByRole[definition.Role] = account;
            items.Add(ToResultItem(definition.Role, "Created", account));
        }

        return Result<BootstrapStandardChartOfAccountsResult>.Success(
            new BootstrapStandardChartOfAccountsResult(
                companyCode,
                items.Count(item => item.Action == "Created"),
                items.Count(item => item.Action == "Reused"),
                items));
    }

    private static BootstrapStandardChartOfAccountsItemResult ToResultItem(
        string role,
        string action,
        LedgerAccount account)
    {
        return new BootstrapStandardChartOfAccountsItemResult(
            account.Id.Value,
            role,
            action,
            account.Code.Value,
            account.Name,
            account.Type.ToString(),
            account.NormalBalance.ToString(),
            account.Level.ToString(),
            account.ParentAccountId?.Value,
            account.IsPostingAccount,
            account.Status.ToString());
    }

    private static Guid? ResolveParentAccountId(
        StandardLedgerAccountDefinition definition,
        IReadOnlyDictionary<string, LedgerAccount> accountsByRole)
    {
        return definition.ParentRole is not null
            && accountsByRole.TryGetValue(definition.ParentRole, out var parent)
            ? parent.Id.Value
            : null;
    }

    private sealed record StandardLedgerAccountDefinition(
        string Role,
        string Code,
        string Name,
        string Type,
        string NormalBalance,
        string Level,
        bool IsPostingAccount,
        string? ParentRole = null);
}
