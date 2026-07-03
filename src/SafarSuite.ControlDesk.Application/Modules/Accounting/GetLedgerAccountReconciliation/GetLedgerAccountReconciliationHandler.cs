using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
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
            ReconcileRangeDefaults(account, range, issues);
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

        var expectedLevel = DetermineExpectedLevel(range, account);

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
        if (RequiresNonPostingAccount(account.Level) && account.IsPostingAccount)
        {
            AddIssue(
                issues,
                "Error",
                "NonPostingLevelIsPosting",
                $"{account.Level} accounts cannot be posting accounts.");
        }

        if (RequiresPostingAccount(account.Level) && !account.IsPostingAccount)
        {
            AddIssue(
                issues,
                "Error",
                "PostingLevelIsNonPosting",
                $"{account.Level} accounts must be posting accounts.");
        }

        if (account.Level is LedgerAccountLevel.Header
            or LedgerAccountLevel.Total
            or LedgerAccountLevel.Master
            or LedgerAccountLevel.Control)
        {
            if (account.ParentAccountId.HasValue)
            {
                AddIssue(
                    issues,
                    "Error",
                    "StructureAccountHasParent",
                    $"{account.Level} accounts should not have a parent account.");
            }

            return;
        }

        if (account.Level == LedgerAccountLevel.Subsidiary)
        {
            ReconcileSubsidiaryParent(account, range, accountsById, issues);

            return;
        }

        if (account.Level == LedgerAccountLevel.Detail
            && account.ParentAccountId.HasValue
            && accountsById.TryGetValue(account.ParentAccountId.Value.Value, out var detailParent)
            && detailParent.Level != LedgerAccountLevel.Master)
        {
            AddIssue(
                issues,
                "Error",
                "DetailParentNotMaster",
                "Detail account parent should be a Master account.");
        }
    }

    private static void ReconcileSubsidiaryParent(
        LedgerAccount account,
        AccountCodeRange? range,
        IReadOnlyDictionary<Guid, LedgerAccount> accountsById,
        ICollection<LedgerAccountReconciliationIssueResult> issues)
    {
        if (!account.ParentAccountId.HasValue)
        {
            AddIssue(
                issues,
                "Error",
                "SubsidiaryMissingParent",
                "Subsidiary account is not linked to a Control parent account.");

            return;
        }

        if (!accountsById.TryGetValue(account.ParentAccountId.Value.Value, out var parentAccount))
        {
            AddIssue(
                issues,
                "Error",
                "SubsidiaryParentMissing",
                "Subsidiary account parent does not exist.");

            return;
        }

        if (parentAccount.Level != LedgerAccountLevel.Control)
        {
            AddIssue(
                issues,
                "Error",
                "SubsidiaryParentNotControl",
                "Subsidiary account parent should be a Control account.");
        }

        if (!string.IsNullOrWhiteSpace(range?.ParentCode)
            && !string.Equals(parentAccount.Code.Value, range.ParentCode, StringComparison.Ordinal))
        {
            AddIssue(
                issues,
                "Error",
                "SubsidiaryParentCodeMismatch",
                $"Subsidiary parent should use configured parent code {range.ParentCode}.");
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
        LedgerAccount account)
    {
        if (HasRangeIntent(range, "Header"))
        {
            return LedgerAccountLevel.Header;
        }

        if (HasRangeIntent(range, "Total"))
        {
            return LedgerAccountLevel.Total;
        }

        if (HasRangeIntent(range, "Control"))
        {
            return LedgerAccountLevel.Control;
        }

        if (HasRangeIntent(range, "Master"))
        {
            return LedgerAccountLevel.Master;
        }

        if (!string.IsNullOrWhiteSpace(range.ParentCode))
        {
            return LedgerAccountLevel.Subsidiary;
        }

        return account.IsPostingAccount
            ? LedgerAccountLevel.Detail
            : LedgerAccountLevel.Master;
    }

    private static bool RequiresNonPostingAccount(LedgerAccountLevel level)
    {
        return level is LedgerAccountLevel.Header
            or LedgerAccountLevel.Total
            or LedgerAccountLevel.Master
            or LedgerAccountLevel.Control;
    }

    private static bool RequiresPostingAccount(LedgerAccountLevel level)
    {
        return level is LedgerAccountLevel.Detail
            or LedgerAccountLevel.Subsidiary;
    }

    private static bool HasRangeIntent(AccountCodeRange range, string intent)
    {
        return range.Role.Contains(intent, StringComparison.OrdinalIgnoreCase)
            || range.DisplayName.Contains(intent, StringComparison.OrdinalIgnoreCase);
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
