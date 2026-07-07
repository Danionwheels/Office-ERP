using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountCodeRangeValidation;

public sealed class GetAccountCodeRangeValidationHandler
{
    private const string ErrorSeverity = "Error";
    private const string WarningSeverity = "Warning";

    private static readonly IReadOnlyCollection<RequiredAccountClass> RequiredAccountClasses =
    [
        new("Asset", LedgerAccountType.Asset, "10000", "19999"),
        new("Equity", LedgerAccountType.Equity, "20000", "29999"),
        new("Liability", LedgerAccountType.Liability, "30000", "39999"),
        new("Revenue", LedgerAccountType.Revenue, "40000", "59999"),
        new("Expense", LedgerAccountType.Expense, "60000", "99999")
    ];

    private readonly IAccountCodeRangeRepository _ranges;
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly AccountingSetupDefaults _defaults;

    public GetAccountCodeRangeValidationHandler(
        IAccountCodeRangeRepository ranges,
        ILedgerAccountRepository ledgerAccounts,
        AccountingSetupDefaults defaults)
    {
        _ranges = ranges;
        _ledgerAccounts = ledgerAccounts;
        _defaults = defaults;
    }

    public async Task<Result<GetAccountCodeRangeValidationResult>> HandleAsync(
        GetAccountCodeRangeValidationQuery query,
        CancellationToken cancellationToken = default)
    {
        var companyError = AccountingSetupDefaults.ValidateSingleCompanyCode(
            query.CompanyCode,
            nameof(query.CompanyCode));

        if (companyError is not null)
        {
            return Result<GetAccountCodeRangeValidationResult>.Failure(companyError);
        }

        var companyCode = AccountingSetupDefaults.NormalizeCompanyCode(query.CompanyCode);
        await _defaults.EnsureSeededAsync(companyCode, cancellationToken);

        var ranges = (await _ranges.ListByCompanyAsync(companyCode, cancellationToken))
            .OrderBy(range => range.CodeLength)
            .ThenBy(range => range.RangeStart, StringComparer.Ordinal)
            .ThenBy(range => range.Role, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var activeRanges = ranges.Where(range => range.IsActive).ToArray();
        var ledgerAccounts = await _ledgerAccounts.ListAsync(cancellationToken: cancellationToken);
        var accountsByCode = ledgerAccounts.ToDictionary(
            account => account.Code.Value,
            StringComparer.OrdinalIgnoreCase);
        var issues = new List<AccountCodeRangeValidationIssueResult>();

        ValidateDuplicateRoles(activeRanges, issues);
        ValidateOverlaps(activeRanges, issues);
        ValidateConfusingCoverage(activeRanges, issues);
        ValidatePostingIntent(activeRanges, issues);
        ValidateParentRules(activeRanges, accountsByCode, issues);
        ValidateRequiredAccountClasses(activeRanges, issues);

        var errorCount = issues.Count(issue =>
            issue.Severity.Equals(ErrorSeverity, StringComparison.OrdinalIgnoreCase));
        var warningCount = issues.Count(issue =>
            issue.Severity.Equals(WarningSeverity, StringComparison.OrdinalIgnoreCase));

        return Result<GetAccountCodeRangeValidationResult>.Success(
            new GetAccountCodeRangeValidationResult(
                companyCode,
                ranges.Length,
                activeRanges.Length,
                errorCount == 0,
                errorCount,
                warningCount,
                issues.Count,
                issues));
    }

    private static void ValidateDuplicateRoles(
        IReadOnlyCollection<AccountCodeRange> activeRanges,
        List<AccountCodeRangeValidationIssueResult> issues)
    {
        foreach (var group in activeRanges.GroupBy(range => range.Role, StringComparer.OrdinalIgnoreCase))
        {
            if (group.Count() <= 1)
            {
                continue;
            }

            var range = group.First();
            AddIssue(
                issues,
                ErrorSeverity,
                "DuplicateRangeRole",
                $"Role {range.Role} is assigned to more than one active account-code range.",
                range);
        }
    }

    private static void ValidateOverlaps(
        IReadOnlyCollection<AccountCodeRange> activeRanges,
        List<AccountCodeRangeValidationIssueResult> issues)
    {
        var ranges = activeRanges.ToArray();

        for (var leftIndex = 0; leftIndex < ranges.Length; leftIndex++)
        {
            for (var rightIndex = leftIndex + 1; rightIndex < ranges.Length; rightIndex++)
            {
                var left = ranges[leftIndex];
                var right = ranges[rightIndex];

                if (left.CodeLength != right.CodeLength || !RangesOverlap(left, right))
                {
                    continue;
                }

                AddIssue(
                    issues,
                    ErrorSeverity,
                    "OverlappingRange",
                    $"{left.Role} overlaps {right.Role}; one account code could match both ranges.",
                    left,
                    right);
            }
        }
    }

    private static void ValidateConfusingCoverage(
        IReadOnlyCollection<AccountCodeRange> activeRanges,
        List<AccountCodeRangeValidationIssueResult> issues)
    {
        var groups = activeRanges
            .GroupBy(range => new
            {
                range.CodeLength,
                range.SearchPrefix,
                range.AccountType,
                range.NormalBalance,
                range.IsPostingAccount,
                ParentCode = range.ParentCode ?? string.Empty
            })
            .Where(group => group.Count() > 1);

        foreach (var group in groups)
        {
            var first = group.First();
            var related = group.Skip(1).First();

            AddIssue(
                issues,
                WarningSeverity,
                "ConfusingRangeCoverage",
                $"{first.Role} and {related.Role} share the same prefix, type, balance, posting flag, and parent rule. Confirm both roles are needed.",
                first,
                related);
        }
    }

    private static void ValidatePostingIntent(
        IReadOnlyCollection<AccountCodeRange> activeRanges,
        List<AccountCodeRangeValidationIssueResult> issues)
    {
        foreach (var range in activeRanges)
        {
            if (HasAnyRangeIntent(range, "Header", "Total", "Master", "Control")
                && range.IsPostingAccount)
            {
                AddIssue(
                    issues,
                    ErrorSeverity,
                    "PostingLevelConflict",
                    $"{range.DisplayName} looks like a non-posting grouping range but is configured as posting.",
                    range);
            }

            if (HasAnyRangeIntent(range, "Detail", "Subsidiary")
                && !range.IsPostingAccount)
            {
                AddIssue(
                    issues,
                    ErrorSeverity,
                    "PostingLevelConflict",
                    $"{range.DisplayName} looks like a posting range but is configured as non-posting.",
                    range);
            }

            if (!string.IsNullOrWhiteSpace(range.ParentCode)
                && !range.IsPostingAccount)
            {
                AddIssue(
                    issues,
                    WarningSeverity,
                    "NestedNonPostingRangeReview",
                    $"{range.DisplayName} is nested under a parent code but configured as non-posting. Confirm the rollup behavior before import.",
                    range);
            }
        }
    }

    private static void ValidateParentRules(
        IReadOnlyCollection<AccountCodeRange> activeRanges,
        IReadOnlyDictionary<string, LedgerAccount> accountsByCode,
        List<AccountCodeRangeValidationIssueResult> issues)
    {
        foreach (var range in activeRanges.Where(range => !string.IsNullOrWhiteSpace(range.ParentCode)))
        {
            var parentCode = range.ParentCode ?? string.Empty;
            var parentRange = activeRanges.FirstOrDefault(candidate =>
                candidate.CodeLength == parentCode.Length
                && IsInsideRange(parentCode, candidate));
            var parentAccountExists = accountsByCode.TryGetValue(parentCode, out var parentAccount);

            if (parentRange is null)
            {
                AddIssue(
                    issues,
                    ErrorSeverity,
                    "ParentCodeNotCovered",
                    $"{range.DisplayName} points to parent code {parentCode}, but no active setup range covers that parent code.",
                    range);
            }
            else
            {
                if (parentRange.IsPostingAccount)
                {
                    AddIssue(
                        issues,
                        WarningSeverity,
                        "ParentRangePostingReview",
                        $"{range.DisplayName} points to {parentCode}, and {parentRange.DisplayName} is configured as posting. Confirm this parent should own nested child accounts.",
                        range,
                        parentRange);
                }

                if (range.IsPostingAccount
                    && !HasAnyRangeIntent(parentRange, "Control", "Master", "Header", "Total", "Detail", "Subsidiary"))
                {
                    AddIssue(
                        issues,
                        WarningSeverity,
                        "ParentRangeLevelReview",
                        $"{range.DisplayName} is a child posting range; confirm parent range {parentRange.DisplayName} is intended to own children.",
                        range,
                        parentRange);
                }
            }

            if (!parentAccountExists)
            {
                AddIssue(
                    issues,
                    WarningSeverity,
                    "ParentAccountMissing",
                    $"{range.DisplayName} points to parent code {parentCode}, but that parent account does not exist in the COA yet.",
                    range,
                    parentRange);
            }
            else if (range.IsPostingAccount && parentAccount!.IsPostingAccount)
            {
                AddIssue(
                    issues,
                    WarningSeverity,
                    "ParentAccountPostingReview",
                    $"{range.DisplayName} points to posting parent account {parentCode}. Confirm this account should own nested child accounts.",
                    range,
                    parentRange);
            }
        }
    }

    private static void ValidateRequiredAccountClasses(
        IReadOnlyCollection<AccountCodeRange> activeRanges,
        List<AccountCodeRangeValidationIssueResult> issues)
    {
        foreach (var accountClass in RequiredAccountClasses)
        {
            var classRanges = activeRanges
                .Where(range => range.AccountType == accountClass.Type
                    && range.CodeLength == accountClass.RangeStart.Length
                    && StringComparer.Ordinal.Compare(range.RangeEnd, accountClass.RangeStart) >= 0
                    && StringComparer.Ordinal.Compare(range.RangeStart, accountClass.RangeEnd) <= 0)
                .ToArray();

            if (classRanges.Length == 0)
            {
                AddIssue(
                    issues,
                    WarningSeverity,
                    "RequiredAccountClassMissing",
                    $"{accountClass.Label} has no active account-code range in {accountClass.RangeStart}-{accountClass.RangeEnd}.",
                    null);
                continue;
            }

            if (!classRanges.Any(range => HasRangeIntent(range, "Header")))
            {
                AddIssue(
                    issues,
                    WarningSeverity,
                    "MissingHeaderRange",
                    $"{accountClass.Label} has no active Header range.",
                    classRanges[0]);
            }

            if (!classRanges.Any(range => HasRangeIntent(range, "Total")))
            {
                AddIssue(
                    issues,
                    WarningSeverity,
                    "MissingTotalRange",
                    $"{accountClass.Label} has no active Total range.",
                    classRanges[^1]);
            }
        }
    }

    private static bool RangesOverlap(AccountCodeRange left, AccountCodeRange right)
    {
        return StringComparer.Ordinal.Compare(left.RangeStart, right.RangeEnd) <= 0
            && StringComparer.Ordinal.Compare(right.RangeStart, left.RangeEnd) <= 0;
    }

    private static bool IsInsideRange(string code, AccountCodeRange range)
    {
        return code.Length == range.CodeLength
            && code.StartsWith(range.SearchPrefix, StringComparison.Ordinal)
            && StringComparer.Ordinal.Compare(code, range.RangeStart) >= 0
            && StringComparer.Ordinal.Compare(code, range.RangeEnd) <= 0;
    }

    private static bool HasAnyRangeIntent(AccountCodeRange range, params string[] intents)
    {
        return intents.Any(intent => HasRangeIntent(range, intent));
    }

    private static bool HasRangeIntent(AccountCodeRange range, string intent)
    {
        return range.Role.Contains(intent, StringComparison.OrdinalIgnoreCase)
            || range.DisplayName.Contains(intent, StringComparison.OrdinalIgnoreCase);
    }

    private static void AddIssue(
        List<AccountCodeRangeValidationIssueResult> issues,
        string severity,
        string code,
        string message,
        AccountCodeRange? range,
        AccountCodeRange? relatedRange = null)
    {
        issues.Add(new AccountCodeRangeValidationIssueResult(
            severity,
            code,
            message,
            range?.Role,
            relatedRange?.Role,
            range?.RangeStart,
            range?.RangeEnd));
    }

    private sealed record RequiredAccountClass(
        string Label,
        LedgerAccountType Type,
        string RangeStart,
        string RangeEnd);
}
