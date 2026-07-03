using System.Numerics;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.SuggestLedgerAccountCode;

public sealed class SuggestLedgerAccountCodeHandler
{
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly IAccountCodeRangeRepository _accountCodeRanges;
    private readonly AccountingSetupDefaults _defaults;

    public SuggestLedgerAccountCodeHandler(
        ILedgerAccountRepository ledgerAccounts,
        IAccountCodeRangeRepository accountCodeRanges,
        AccountingSetupDefaults defaults)
    {
        _ledgerAccounts = ledgerAccounts;
        _accountCodeRanges = accountCodeRanges;
        _defaults = defaults;
    }

    public async Task<Result<SuggestLedgerAccountCodeResult>> HandleAsync(
        SuggestLedgerAccountCodeQuery query,
        CancellationToken cancellationToken = default)
    {
        var companyError = AccountingSetupDefaults.ValidateSingleCompanyCode(
            query.CompanyCode,
            nameof(query.CompanyCode));

        if (companyError is not null)
        {
            return Result<SuggestLedgerAccountCodeResult>.Failure(companyError);
        }

        var companyCode = AccountingSetupDefaults.NormalizeCompanyCode(query.CompanyCode);
        await _defaults.EnsureSeededAsync(companyCode, cancellationToken);

        var range = await _accountCodeRanges.GetByCompanyAndRoleAsync(
            companyCode,
            query.Role.Trim(),
            cancellationToken);

        if (range is null || !range.IsActive)
        {
            return Result<SuggestLedgerAccountCodeResult>.Failure(ApplicationError.Validation(
                nameof(query.Role),
                "Ledger account code role is not supported."));
        }

        var existingCodes = await _ledgerAccounts.ListCodesByPrefixAsync(
            range.SearchPrefix,
            cancellationToken);
        var usedCodes = existingCodes
            .Select(OnlyDigits)
            .Where(code => code.Length == range.CodeLength)
            .ToHashSet(StringComparer.Ordinal);

        var start = BigInteger.Parse(range.RangeStart);
        var end = BigInteger.Parse(range.RangeEnd);

        for (var value = start; value <= end; value++)
        {
            var candidate = value.ToString().PadLeft(range.CodeLength, '0');

            if (usedCodes.Contains(candidate))
            {
                continue;
            }

            return Result<SuggestLedgerAccountCodeResult>.Success(new SuggestLedgerAccountCodeResult(
                range.CompanyCode,
                range.Role,
                candidate,
                range.FormatDisplayCode(candidate),
                range.AccountType.ToString(),
                range.NormalBalance.ToString(),
                range.IsPostingAccount,
                range.RangeStart,
                range.RangeEnd,
                range.ParentCode));
        }

        return Result<SuggestLedgerAccountCodeResult>.Failure(ApplicationError.Conflict(
            nameof(query.Role),
            $"No ledger account code is available for role {range.Role}."));
    }

    private static string OnlyDigits(string value)
    {
        return new string(value.Where(char.IsDigit).ToArray());
    }
}
