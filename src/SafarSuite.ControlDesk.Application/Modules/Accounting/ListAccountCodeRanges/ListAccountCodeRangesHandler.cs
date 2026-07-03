using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ListAccountCodeRanges;

public sealed class ListAccountCodeRangesHandler
{
    private readonly IAccountCodeRangeRepository _ranges;
    private readonly AccountingSetupDefaults _defaults;

    public ListAccountCodeRangesHandler(
        IAccountCodeRangeRepository ranges,
        AccountingSetupDefaults defaults)
    {
        _ranges = ranges;
        _defaults = defaults;
    }

    public async Task<Result<ListAccountCodeRangesResult>> HandleAsync(
        ListAccountCodeRangesQuery query,
        CancellationToken cancellationToken = default)
    {
        var companyError = AccountingSetupDefaults.ValidateSingleCompanyCode(
            query.CompanyCode,
            nameof(query.CompanyCode));

        if (companyError is not null)
        {
            return Result<ListAccountCodeRangesResult>.Failure(companyError);
        }

        var companyCode = AccountingSetupDefaults.NormalizeCompanyCode(query.CompanyCode);
        await _defaults.EnsureSeededAsync(companyCode, cancellationToken);

        var ranges = await _ranges.ListByCompanyAsync(companyCode, cancellationToken);

        return Result<ListAccountCodeRangesResult>.Success(new ListAccountCodeRangesResult(
            companyCode,
            ranges
                .OrderBy(range => range.Role, StringComparer.OrdinalIgnoreCase)
                .Select(ToResult)
                .ToArray()));
    }

    private static AccountCodeRangeResult ToResult(AccountCodeRange range)
    {
        return new AccountCodeRangeResult(
            range.Id.Value,
            range.CompanyCode,
            range.Role,
            range.DisplayName,
            range.SearchPrefix,
            range.RangeStart,
            range.RangeEnd,
            range.CodeLength,
            range.AccountType.ToString(),
            range.NormalBalance.ToString(),
            range.IsPostingAccount,
            range.ParentCode,
            range.IsActive);
    }
}
