using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ListLedgerAccounts;

public sealed class ListLedgerAccountsHandler
{
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly IAccountCodeRangeRepository _accountCodeRanges;
    private readonly AccountingSetupDefaults _defaults;

    public ListLedgerAccountsHandler(
        ILedgerAccountRepository ledgerAccounts,
        IAccountCodeRangeRepository accountCodeRanges,
        AccountingSetupDefaults defaults)
    {
        _ledgerAccounts = ledgerAccounts;
        _accountCodeRanges = accountCodeRanges;
        _defaults = defaults;
    }

    public async Task<Result<ListLedgerAccountsResult>> HandleAsync(
        ListLedgerAccountsQuery query,
        CancellationToken cancellationToken = default)
    {
        LedgerAccountType? type = null;
        LedgerAccountStatus? status = null;

        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            if (!Enum.TryParse<LedgerAccountType>(query.Type, true, out var parsedType)
                || !Enum.IsDefined(parsedType))
            {
                return Result<ListLedgerAccountsResult>.Failure(ApplicationError.Validation(
                    nameof(query.Type),
                    "Ledger account type is invalid."));
            }

            type = parsedType;
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<LedgerAccountStatus>(query.Status, true, out var parsedStatus)
                || !Enum.IsDefined(parsedStatus))
            {
                return Result<ListLedgerAccountsResult>.Failure(ApplicationError.Validation(
                    nameof(query.Status),
                    "Ledger account status is invalid."));
            }

            status = parsedStatus;
        }

        var companyError = AccountingSetupDefaults.ValidateSingleCompanyCode(
            query.CompanyCode,
            nameof(query.CompanyCode));

        if (companyError is not null)
        {
            return Result<ListLedgerAccountsResult>.Failure(companyError);
        }

        var companyCode = AccountingSetupDefaults.NormalizeCompanyCode(query.CompanyCode);
        await _defaults.EnsureSeededAsync(companyCode, cancellationToken);

        var isParentTreeView = IsParentTreeView(query.ViewMode);
        var accounts = await _ledgerAccounts.ListAsync(
            isParentTreeView ? null : query.Search,
            type,
            status,
            isParentTreeView ? null : query.IsPostingAccount,
            cancellationToken);
        var ranges = (await _accountCodeRanges.ListByCompanyAsync(companyCode, cancellationToken))
            .Where(range => range.IsActive)
            .ToArray();
        var normalizedRole = string.IsNullOrWhiteSpace(query.Role)
            ? null
            : query.Role.Trim();

        var summaries = accounts
            .Select(account => ToSummary(account, FindRange(account.Code.Value, ranges)))
            .ToArray();

        if (isParentTreeView)
        {
            summaries = FilterParentTreeSummaries(summaries, query.Search, normalizedRole);
        }
        else if (normalizedRole is not null)
        {
            summaries = summaries
                .Where(account => string.Equals(account.RangeRole, normalizedRole, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        return Result<ListLedgerAccountsResult>.Success(new ListLedgerAccountsResult(
            companyCode,
            summaries));
    }

    private static LedgerAccountSummaryResult ToSummary(
        LedgerAccount account,
        AccountCodeRange? range)
    {
        return new LedgerAccountSummaryResult(
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
            account.CreatedAtUtc,
            range?.Role,
            range?.DisplayName);
    }

    private static AccountCodeRange? FindRange(
        string code,
        IReadOnlyCollection<AccountCodeRange> ranges)
    {
        return ranges.FirstOrDefault(range =>
            code.Length == range.CodeLength
            && code.StartsWith(range.SearchPrefix, StringComparison.Ordinal)
            && StringComparer.Ordinal.Compare(code, range.RangeStart) >= 0
            && StringComparer.Ordinal.Compare(code, range.RangeEnd) <= 0);
    }

    private static bool IsParentTreeView(string? viewMode)
    {
        return string.Equals(viewMode?.Trim(), "headerTotal", StringComparison.OrdinalIgnoreCase)
            || string.Equals(viewMode?.Trim(), "parentTree", StringComparison.OrdinalIgnoreCase);
    }

    private static LedgerAccountSummaryResult[] FilterParentTreeSummaries(
        IReadOnlyCollection<LedgerAccountSummaryResult> summaries,
        string? search,
        string? role)
    {
        var roots = summaries
            .Where(account => string.Equals(account.Level, "Total", StringComparison.OrdinalIgnoreCase))
            .OrderBy(account => account.Code, StringComparer.Ordinal)
            .ThenBy(account => account.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (roots.Length == 0)
        {
            return summaries.ToArray();
        }

        var includedIds = new HashSet<Guid>();
        var normalizedSearch = search?.Trim();
        var normalizedRole = role?.Trim();

        foreach (var root in roots)
        {
            var rootTree = summaries
                .Where(candidate => BelongsToParentTreeRoot(candidate, root, summaries))
                .ToArray();
            var matchesSearch = string.IsNullOrWhiteSpace(normalizedSearch)
                || rootTree.Any(candidate => MatchesSearch(candidate, normalizedSearch));
            var matchesRole = string.IsNullOrWhiteSpace(normalizedRole)
                || rootTree.Any(candidate => MatchesRole(candidate, normalizedRole));

            if (!matchesSearch || !matchesRole)
            {
                continue;
            }

            foreach (var account in rootTree)
            {
                includedIds.Add(account.LedgerAccountId);
            }
        }

        return summaries
            .Where(account => includedIds.Contains(account.LedgerAccountId))
            .ToArray();
    }

    private static bool BelongsToParentTreeRoot(
        LedgerAccountSummaryResult account,
        LedgerAccountSummaryResult root,
        IReadOnlyCollection<LedgerAccountSummaryResult> summaries)
    {
        if (account.LedgerAccountId == root.LedgerAccountId)
        {
            return true;
        }

        var accountsById = summaries.ToDictionary(candidate => candidate.LedgerAccountId);
        var nextParentId = account.ParentAccountId;
        var guard = 0;

        while (nextParentId.HasValue && guard < 24)
        {
            if (nextParentId.Value == root.LedgerAccountId)
            {
                return true;
            }

            nextParentId = accountsById.TryGetValue(nextParentId.Value, out var parent)
                ? parent.ParentAccountId
                : null;
            guard++;
        }

        if (account.Code.StartsWith(root.Code, StringComparison.Ordinal)
            && account.Code.Length > root.Code.Length)
        {
            return true;
        }

        return !string.Equals(account.Level, "Header", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(account.Level, "Total", StringComparison.OrdinalIgnoreCase)
            && string.Equals(account.Type, root.Type, StringComparison.OrdinalIgnoreCase)
            && string.Equals(account.NormalBalance, root.NormalBalance, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesSearch(LedgerAccountSummaryResult account, string search)
    {
        return account.Code.Contains(search, StringComparison.OrdinalIgnoreCase)
            || account.DisplayCode.Contains(search, StringComparison.OrdinalIgnoreCase)
            || account.Name.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesRole(LedgerAccountSummaryResult account, string role)
    {
        return string.Equals(account.RangeRole, role, StringComparison.OrdinalIgnoreCase);
    }
}
