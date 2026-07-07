using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountReconciliation;

public sealed class GetLedgerAccountReconciliationHandler
{
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly IAccountCodeRangeRepository _accountCodeRanges;
    private readonly AccountingSetupDefaults _defaults;

    public GetLedgerAccountReconciliationHandler(
        ILedgerAccountRepository ledgerAccounts,
        IAccountCodeRangeRepository accountCodeRanges,
        AccountingSetupDefaults defaults)
    {
        _ledgerAccounts = ledgerAccounts;
        _accountCodeRanges = accountCodeRanges;
        _defaults = defaults;
    }

    public async Task<Result<GetLedgerAccountReconciliationResult>> HandleAsync(
        GetLedgerAccountReconciliationQuery query,
        CancellationToken cancellationToken = default)
    {
        var companyError = AccountingSetupDefaults.ValidateSingleCompanyCode(
            query.CompanyCode,
            nameof(query.CompanyCode));

        if (companyError is not null)
        {
            return Result<GetLedgerAccountReconciliationResult>.Failure(companyError);
        }

        var companyCode = AccountingSetupDefaults.NormalizeCompanyCode(query.CompanyCode);
        await _defaults.EnsureSeededAsync(companyCode, cancellationToken);

        var accounts = (await _ledgerAccounts.ListAsync(cancellationToken: cancellationToken))
            .OrderBy(account => account.Code.Value, StringComparer.Ordinal)
            .ToArray();
        var ranges = await _accountCodeRanges.ListByCompanyAsync(companyCode, cancellationToken);
        var accountsById = accounts.ToDictionary(account => account.Id.Value);
        var items = accounts
            .Select(account => ReconcileAccount(account, ranges, accountsById))
            .Where(item => item.Issues.Count > 0)
            .ToArray();

        return Result<GetLedgerAccountReconciliationResult>.Success(
            new GetLedgerAccountReconciliationResult(
                companyCode,
                accounts.Length,
                items.Sum(item => item.Issues.Count),
                items));
    }

    private static LedgerAccountReconciliationItemResult ReconcileAccount(
        LedgerAccount account,
        IReadOnlyCollection<AccountCodeRange> ranges,
        IReadOnlyDictionary<Guid, LedgerAccount> accountsById)
    {
        var range = FindRange(account.Code.Value, ranges);
        var issues = new List<LedgerAccountReconciliationIssueResult>();

        if (range is null)
        {
            AddIssue(
                issues,
                "Error",
                "AccountOutsideSetupRange",
                "Account code is outside the current Accounting Setup ranges.");
        }
        else
        {
            ReconcileRangeDefaults(account, range, accountsById, issues);
        }

        ReconcileLevelRules(account, range, accountsById, issues);

        return new LedgerAccountReconciliationItemResult(
            account.Id.Value,
            account.Code.Value,
            range?.FormatDisplayCode(account.Code.Value) ?? account.Code.Value,
            account.Name,
            account.Type.ToString(),
            account.NormalBalance.ToString(),
            account.Level.ToString(),
            account.ParentAccountId?.Value,
            account.IsPostingAccount,
            account.Status.ToString(),
            range?.Role,
            range?.DisplayName,
            issues);
    }

    private static void ReconcileRangeDefaults(
        LedgerAccount account,
        AccountCodeRange range,
        IReadOnlyDictionary<Guid, LedgerAccount> accountsById,
        ICollection<LedgerAccountReconciliationIssueResult> issues)
    {
        if (!range.IsActive)
        {
            AddIssue(
                issues,
                "Warning",
                "MatchedRangeInactive",
                $"Matched range {range.DisplayName} is inactive.");
        }

        if (account.Type != range.AccountType)
        {
            AddIssue(
                issues,
                "Error",
                "AccountTypeMismatch",
                $"Account type is {account.Type}, but range {range.DisplayName} expects {range.AccountType}.");
        }

        if (account.NormalBalance != range.NormalBalance)
        {
            AddIssue(
                issues,
                "Error",
                "NormalBalanceMismatch",
                $"Normal balance is {account.NormalBalance}, but range {range.DisplayName} expects {range.NormalBalance}.");
        }

        if (account.IsPostingAccount != range.IsPostingAccount)
        {
            AddIssue(
                issues,
                "Error",
                "PostingFlagMismatch",
                $"Posting flag is {account.IsPostingAccount}, but range {range.DisplayName} expects {range.IsPostingAccount}.");
        }

        var expectedLevel = DetermineExpectedLevel(range, account, accountsById);

        if (account.Level != expectedLevel)
        {
            AddIssue(
                issues,
                "Error",
                "AccountLevelMismatch",
                $"Account level is {account.Level}, but range {range.DisplayName} expects {expectedLevel}.");
        }
    }

    private static void ReconcileLevelRules(
        LedgerAccount account,
        AccountCodeRange? range,
        IReadOnlyDictionary<Guid, LedgerAccount> accountsById,
        ICollection<LedgerAccountReconciliationIssueResult> issues)
    {
        if (LedgerAccountHierarchyPolicy.RequiresNonPostingAccount(account.Level)
            && account.IsPostingAccount)
        {
            AddIssue(
                issues,
                "Error",
                "NonPostingLevelIsPosting",
                $"{account.Level} accounts cannot be posting accounts.");
        }

        if (LedgerAccountHierarchyPolicy.RequiresPostingAccount(account.Level)
            && !account.IsPostingAccount)
        {
            AddIssue(
                issues,
                "Error",
                "PostingLevelIsNonPosting",
                $"{account.Level} accounts must be posting accounts.");
        }

        if (LedgerAccountHierarchyPolicy.IsStructuralLevel(account.Level))
        {
            ReconcileStructuralParent(account, accountsById, issues);

            return;
        }

        if (LedgerAccountHierarchyPolicy.RequiresPostingAccount(account.Level))
        {
            ReconcilePostingParent(account, range, accountsById, issues);
        }
    }

    private static void ReconcileStructuralParent(
        LedgerAccount account,
        IReadOnlyDictionary<Guid, LedgerAccount> accountsById,
        ICollection<LedgerAccountReconciliationIssueResult> issues)
    {
        if (!account.ParentAccountId.HasValue)
        {
            return;
        }

        if (!accountsById.TryGetValue(account.ParentAccountId.Value.Value, out var parentAccount))
        {
            AddIssue(
                issues,
                "Error",
                "StructuralParentMissing",
                "Structural account parent does not exist.");

            return;
        }

        if (!LedgerAccountHierarchyPolicy.IsStructuralLevel(parentAccount.Level)
            || parentAccount.IsPostingAccount)
        {
            AddIssue(
                issues,
                "Error",
                "StructuralParentNotStructural",
                "Structural account parent should be a non-posting Header, Total, Master, or Control account.");
        }

        if (parentAccount.Type != account.Type || parentAccount.NormalBalance != account.NormalBalance)
        {
            AddIssue(
                issues,
                "Error",
                "StructuralParentTypeBalanceMismatch",
                "Structural account parent should use the same account type and normal balance.");
        }
    }

    private static void ReconcilePostingParent(
        LedgerAccount account,
        AccountCodeRange? range,
        IReadOnlyDictionary<Guid, LedgerAccount> accountsById,
        ICollection<LedgerAccountReconciliationIssueResult> issues)
    {
        if (!account.ParentAccountId.HasValue)
        {
            if (account.Level == LedgerAccountLevel.Subsidiary)
            {
                AddIssue(
                    issues,
                    "Error",
                    "PostingChildMissingParent",
                    "Subsidiary account is not linked to a parent account.");
            }

            return;
        }

        if (!accountsById.TryGetValue(account.ParentAccountId.Value.Value, out var parentAccount))
        {
            AddIssue(
                issues,
                "Error",
                "PostingChildParentMissing",
                "Posting account parent does not exist.");

            return;
        }

        if (parentAccount.Status != LedgerAccountStatus.Active)
        {
            AddIssue(
                issues,
                "Warning",
                "PostingChildParentInactive",
                "Posting account parent is inactive.");
        }

        if (parentAccount.Type != account.Type || parentAccount.NormalBalance != account.NormalBalance)
        {
            AddIssue(
                issues,
                "Error",
                "PostingChildParentTypeBalanceMismatch",
                "Posting account parent should use the same account type and normal balance.");
        }

        if (range is not null
            && !LedgerAccountHierarchyPolicy.IsChildCodeInsideParentScope(
                account.Code.Value,
                parentAccount.Code.Value,
                range))
        {
            var issueCode = string.IsNullOrWhiteSpace(range.ParentCode)
                ? "PostingChildParentCodeScopeMismatch"
                : "PostingChildParentCodeMismatch";
            AddIssue(
                issues,
                "Error",
                issueCode,
                $"Posting parent {parentAccount.Code.Value} cannot own codes from range {range.DisplayName}.");
        }
    }

    private static AccountCodeRange? FindRange(
        string code,
        IReadOnlyCollection<AccountCodeRange> ranges)
    {
        return ranges
            .Where(range =>
                code.Length == range.CodeLength
                && code.StartsWith(range.SearchPrefix, StringComparison.Ordinal)
                && StringComparer.Ordinal.Compare(code, range.RangeStart) >= 0
                && StringComparer.Ordinal.Compare(code, range.RangeEnd) <= 0)
            .OrderByDescending(range => range.IsActive)
            .ThenByDescending(range => range.SearchPrefix.Length)
            .FirstOrDefault();
    }

    private static LedgerAccountLevel DetermineExpectedLevel(
        AccountCodeRange range,
        LedgerAccount account,
        IReadOnlyDictionary<Guid, LedgerAccount> accountsById)
    {
        accountsById.TryGetValue(
            account.ParentAccountId?.Value ?? Guid.Empty,
            out var parentAccount);

        return LedgerAccountHierarchyPolicy.DetermineExpectedLevel(
            range,
            parentAccount,
            account.IsPostingAccount);
    }

    private static void AddIssue(
        ICollection<LedgerAccountReconciliationIssueResult> issues,
        string severity,
        string code,
        string message)
    {
        issues.Add(new LedgerAccountReconciliationIssueResult(severity, code, message));
    }
}
