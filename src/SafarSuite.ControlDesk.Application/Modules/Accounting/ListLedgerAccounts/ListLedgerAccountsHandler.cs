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

        var accounts = await _ledgerAccounts.ListAsync(
            query.Search,
            type,
            status,
            query.IsPostingAccount,
            cancellationToken);
        var ranges = (await _accountCodeRanges.ListByCompanyAsync(companyCode, cancellationToken))
            .Where(range => range.IsActive)
            .ToArray();
        var normalizedRole = string.IsNullOrWhiteSpace(query.Role)
            ? null
            : query.Role.Trim();

        var summaries = accounts
            .Select(account => ToSummary(account, FindRange(account.Code.Value, ranges)))
            .Where(account => normalizedRole is null
                || string.Equals(account.RangeRole, normalizedRole, StringComparison.OrdinalIgnoreCase))
            .ToArray();

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
}
