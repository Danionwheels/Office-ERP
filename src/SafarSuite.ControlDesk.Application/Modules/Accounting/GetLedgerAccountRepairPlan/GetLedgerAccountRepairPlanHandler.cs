using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountReconciliation;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountRepairPlan;

public sealed class GetLedgerAccountRepairPlanHandler
{
    private readonly GetLedgerAccountReconciliationHandler _reconciliation;
    private readonly IAccountCodeRangeRepository _accountCodeRanges;
    private readonly ILedgerAccountRepository _ledgerAccounts;

    public GetLedgerAccountRepairPlanHandler(
        GetLedgerAccountReconciliationHandler reconciliation,
        IAccountCodeRangeRepository accountCodeRanges,
        ILedgerAccountRepository ledgerAccounts)
    {
        _reconciliation = reconciliation;
        _accountCodeRanges = accountCodeRanges;
        _ledgerAccounts = ledgerAccounts;
    }

    public async Task<Result<GetLedgerAccountRepairPlanResult>> HandleAsync(
        GetLedgerAccountRepairPlanQuery query,
        CancellationToken cancellationToken = default)
    {
        var companyError = AccountingSetupDefaults.ValidateSingleCompanyCode(
            query.CompanyCode,
            nameof(query.CompanyCode));

        if (companyError is not null)
        {
            return Result<GetLedgerAccountRepairPlanResult>.Failure(companyError);
        }

        var companyCode = AccountingSetupDefaults.NormalizeCompanyCode(query.CompanyCode);
        var reconciliation = await _reconciliation.HandleAsync(
            new GetLedgerAccountReconciliationQuery(companyCode),
            cancellationToken);

        if (reconciliation.IsFailure)
        {
            return Result<GetLedgerAccountRepairPlanResult>.Failure(reconciliation.Errors);
        }

        var ranges = await _accountCodeRanges.ListByCompanyAsync(companyCode, cancellationToken);
        var accounts = await _ledgerAccounts.ListAsync(cancellationToken: cancellationToken);
        var accountsById = accounts.ToDictionary(account => account.Id.Value);
        var rangesByRole = ranges
            .GroupBy(range => range.Role, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var planItems = reconciliation.Value.Items
            .Select(item => BuildPlanItem(item, rangesByRole, accountsById))
            .Where(item => item.Actions.Count > 0)
            .ToArray();

        return Result<GetLedgerAccountRepairPlanResult>.Success(
            new GetLedgerAccountRepairPlanResult(
                companyCode,
                reconciliation.Value.AccountCount,
                reconciliation.Value.IssueCount,
                planItems.Sum(item => item.Actions.Count),
                planItems));
    }

    private static LedgerAccountRepairPlanItemResult BuildPlanItem(
        LedgerAccountReconciliationItemResult item,
        IReadOnlyDictionary<string, AccountCodeRange> rangesByRole,
        IReadOnlyDictionary<Guid, LedgerAccount> accountsById)
    {
        var range = item.RangeRole is null || !rangesByRole.TryGetValue(item.RangeRole, out var matchedRange)
            ? null
            : matchedRange;
        var actions = item.Issues
            .Select(issue => BuildAction(item, issue, range, accountsById))
            .ToArray();

        return new LedgerAccountRepairPlanItemResult(
            item.LedgerAccountId,
            item.Code,
            item.DisplayCode,
            item.Name,
            item.Type,
            item.NormalBalance,
            item.Level,
            item.ParentAccountId,
            item.IsPostingAccount,
            item.Status,
            item.RangeRole,
            item.RangeDisplayName,
            actions);
    }

    private static LedgerAccountRepairActionResult BuildAction(
        LedgerAccountReconciliationItemResult item,
        LedgerAccountReconciliationIssueResult issue,
        AccountCodeRange? range,
        IReadOnlyDictionary<Guid, LedgerAccount> accountsById)
    {
        return issue.Code switch
        {
            "AccountOutsideSetupRange" => BuildOutsideRangeAction(item, issue),
            "MatchedRangeInactive" => BuildSetupRangeAction(
                issue,
                "Activate or replace inactive range",
                "Review the matched Accounting Setup range. Activate it only if this range is still valid for new posting, otherwise move future use to an active controlled range.",
                "SetupRangeReview",
                true,
                range?.IsActive.ToString(),
                "Active",
                "Existing journals stay untouched.",
                "Do not activate old ranges if they were intentionally retired."),
            "AccountTypeMismatch" => BuildImmutableAccountAction(
                issue,
                "Resolve account type mismatch",
                "The account type is stored as part of the ledger account identity. Prefer a controlled replacement account or correct the setup range if the range definition is wrong.",
                item.Type,
                range?.AccountType.ToString()),
            "NormalBalanceMismatch" => BuildImmutableAccountAction(
                issue,
                "Resolve normal balance mismatch",
                "The normal balance is stored as part of the ledger account identity. Prefer a controlled replacement account or correct the setup range if the range definition is wrong.",
                item.NormalBalance,
                range?.NormalBalance.ToString()),
            "PostingFlagMismatch" => BuildPostingFlagAction(item, issue, range),
            "AccountLevelMismatch" => BuildImmutableAccountAction(
                issue,
                "Resolve hierarchy level mismatch",
                "The account level controls posting and parent rules. Prefer a controlled replacement account or a later guided migration that moves balances deliberately.",
                item.Level,
                range is null ? null : DetermineExpectedLevel(range, item.IsPostingAccount).ToString()),
            "NonPostingLevelIsPosting" => BuildPostingFlagAction(
                item,
                issue,
                "Switch to non-posting",
                "This account level cannot accept direct journal lines. A later guided action can turn posting off after dependency checks.",
                "Posting",
                "Non-posting",
                true),
            "PostingLevelIsNonPosting" => BuildPostingFlagAction(
                item,
                issue,
                "Switch to posting",
                "This account level is expected to accept postings. A later guided action can turn posting on after range checks.",
                "Non-posting",
                "Posting",
                true),
            "StructureAccountHasParent" => BuildParentAction(
                issue,
                "Remove structure parent link",
                "Header, Total, Master, and Control accounts should not have a parent in this setup slice. Parent relinking needs a guided migration because the parent is part of the ledger structure.",
                item.ParentAccountId?.ToString(),
                "No parent",
                false),
            "SubsidiaryMissingParent" => BuildParentAction(
                issue,
                "Link subsidiary to control account",
                "Subsidiary accounts must sit under an existing Control account. Use the configured parent code as the target, then run a guided relink later.",
                "No parent",
                FindSuggestedParent(range, accountsById),
                false),
            "SubsidiaryParentMissing" => BuildParentAction(
                issue,
                "Replace missing parent link",
                "The stored parent account no longer exists. Link the subsidiary to the configured Control parent after confirming historical postings.",
                item.ParentAccountId?.ToString(),
                FindSuggestedParent(range, accountsById),
                false),
            "SubsidiaryParentNotControl" => BuildParentAction(
                issue,
                "Relink subsidiary to control parent",
                "The current parent is not a Control account. Relink only through a guided repair after checking ledger activity.",
                DescribeParent(item.ParentAccountId, accountsById),
                FindSuggestedParent(range, accountsById),
                false),
            "SubsidiaryParentCodeMismatch" => BuildParentAction(
                issue,
                "Relink subsidiary to configured parent code",
                "The subsidiary is linked, but not to the parent code configured for this range. Relink only after reviewing activity.",
                DescribeParent(item.ParentAccountId, accountsById),
                FindSuggestedParent(range, accountsById),
                false),
            "DetailParentNotMaster" => BuildParentAction(
                issue,
                "Use a Master parent for detail account",
                "Detail accounts may only link to Master accounts. Either clear the parent or relink to a valid Master in a guided migration.",
                DescribeParent(item.ParentAccountId, accountsById),
                "Master parent or no parent",
                false),
            _ => BuildReviewOnlyAction(issue)
        };
    }

    private static LedgerAccountRepairActionResult BuildOutsideRangeAction(
        LedgerAccountReconciliationItemResult item,
        LedgerAccountReconciliationIssueResult issue)
    {
        var isNumeric = item.Code.All(char.IsDigit);
        var suggestedValue = isNumeric
            ? "Add/adjust setup range or create replacement inside an active range"
            : "Create controlled numeric replacement account";

        return new LedgerAccountRepairActionResult(
            issue.Code,
            issue.Severity,
            "ReviewAccountCode",
            "Bring account code under Accounting Setup",
            "The account code does not belong to any configured range. Keep historical rows intact, then decide whether to add a deliberate setup range or move future posting to a controlled replacement account.",
            "ManualReview",
            false,
            item.Code,
            suggestedValue,
            [
                "No automatic code changes are allowed because existing journals may reference this account.",
                "Legacy/manual alphanumeric codes should be replaced through controlled numeric setup."
            ]);
    }

    private static LedgerAccountRepairActionResult BuildSetupRangeAction(
        LedgerAccountReconciliationIssueResult issue,
        string title,
        string description,
        string repairMode,
        bool isAutomatable,
        string? currentValue,
        string? suggestedValue,
        params string[] notes)
    {
        return new LedgerAccountRepairActionResult(
            issue.Code,
            issue.Severity,
            "ReviewSetupRange",
            title,
            description,
            repairMode,
            isAutomatable,
            currentValue,
            suggestedValue,
            notes);
    }

    private static LedgerAccountRepairActionResult BuildImmutableAccountAction(
        LedgerAccountReconciliationIssueResult issue,
        string title,
        string description,
        string? currentValue,
        string? suggestedValue)
    {
        return new LedgerAccountRepairActionResult(
            issue.Code,
            issue.Severity,
            "ReplaceOrReviewAccount",
            title,
            description,
            "ReplacementAccount",
            false,
            currentValue,
            suggestedValue,
            [
                "Do not rewrite posted accounting history.",
                "If the setup range is wrong, fix the range before creating more accounts."
            ]);
    }

    private static LedgerAccountRepairActionResult BuildPostingFlagAction(
        LedgerAccountReconciliationItemResult item,
        LedgerAccountReconciliationIssueResult issue,
        AccountCodeRange? range)
    {
        var suggestedPosting = range?.IsPostingAccount;

        return BuildPostingFlagAction(
            item,
            issue,
            "Align posting flag with setup",
            "Posting behavior should follow the matched Accounting Setup range and hierarchy level.",
            item.IsPostingAccount ? "Posting" : "Non-posting",
            suggestedPosting.HasValue
                ? suggestedPosting.Value ? "Posting" : "Non-posting"
                : null,
            suggestedPosting.HasValue && IsPostingFlipCompatible(item, suggestedPosting.Value));
    }

    private static LedgerAccountRepairActionResult BuildPostingFlagAction(
        LedgerAccountReconciliationItemResult item,
        LedgerAccountReconciliationIssueResult issue,
        string title,
        string description,
        string? currentValue,
        string? suggestedValue,
        bool isAutomatable)
    {
        _ = item;

        return new LedgerAccountRepairActionResult(
            issue.Code,
            issue.Severity,
            "UpdatePostingFlag",
            title,
            description,
            isAutomatable ? "FutureGuidedAction" : "ManualReview",
            isAutomatable,
            currentValue,
            suggestedValue,
            [
                "Later repair must check existing journal usage first.",
                "Header, Total, Master, and Control accounts must remain non-posting."
            ]);
    }

    private static LedgerAccountRepairActionResult BuildParentAction(
        LedgerAccountReconciliationIssueResult issue,
        string title,
        string description,
        string? currentValue,
        string? suggestedValue,
        bool isAutomatable)
    {
        return new LedgerAccountRepairActionResult(
            issue.Code,
            issue.Severity,
            "ReviewParentLink",
            title,
            description,
            isAutomatable ? "FutureGuidedAction" : "ManualReview",
            isAutomatable,
            currentValue,
            suggestedValue,
            [
                "Parent changes affect the visible account hierarchy.",
                "Review related ledger activity before enabling a mutating repair."
            ]);
    }

    private static LedgerAccountRepairActionResult BuildReviewOnlyAction(
        LedgerAccountReconciliationIssueResult issue)
    {
        return new LedgerAccountRepairActionResult(
            issue.Code,
            issue.Severity,
            "ReviewOnly",
            "Review reconciliation issue",
            issue.Message,
            "ManualReview",
            false,
            null,
            null,
            ["No guided repair has been mapped for this issue yet."]);
    }

    private static bool IsPostingFlipCompatible(
        LedgerAccountReconciliationItemResult item,
        bool suggestedPosting)
    {
        if (suggestedPosting)
        {
            return item.Level is "Detail" or "Subsidiary";
        }

        return item.Level is "Header" or "Total" or "Master" or "Control";
    }

    private static string? FindSuggestedParent(
        AccountCodeRange? range,
        IReadOnlyDictionary<Guid, LedgerAccount> accountsById)
    {
        if (string.IsNullOrWhiteSpace(range?.ParentCode))
        {
            return "Control parent";
        }

        var parent = accountsById.Values.FirstOrDefault(account =>
            string.Equals(account.Code.Value, range.ParentCode, StringComparison.Ordinal)
            && account.Level == LedgerAccountLevel.Control);

        return parent is null
            ? $"{range.ParentCode} Control account must be created first"
            : $"{parent.Code.Value} - {parent.Name}";
    }

    private static string? DescribeParent(
        Guid? parentAccountId,
        IReadOnlyDictionary<Guid, LedgerAccount> accountsById)
    {
        if (!parentAccountId.HasValue)
        {
            return null;
        }

        return accountsById.TryGetValue(parentAccountId.Value, out var parent)
            ? $"{parent.Code.Value} - {parent.Name} ({parent.Level})"
            : parentAccountId.Value.ToString();
    }

    private static LedgerAccountLevel DetermineExpectedLevel(
        AccountCodeRange range,
        bool isPostingAccount)
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

        return isPostingAccount
            ? LedgerAccountLevel.Detail
            : LedgerAccountLevel.Master;
    }

    private static bool HasRangeIntent(AccountCodeRange range, string intent)
    {
        return range.Role.Contains(intent, StringComparison.OrdinalIgnoreCase)
            || range.DisplayName.Contains(intent, StringComparison.OrdinalIgnoreCase);
    }
}
