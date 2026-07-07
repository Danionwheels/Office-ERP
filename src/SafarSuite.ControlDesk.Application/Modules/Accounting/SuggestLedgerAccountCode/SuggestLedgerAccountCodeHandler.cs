using System.Numerics;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

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

        var parentAccountResolution = await ResolveParentAccountAsync(
            query.ParentAccountId,
            range,
            cancellationToken);

        if (parentAccountResolution.Error is not null)
        {
            return Result<SuggestLedgerAccountCodeResult>.Failure(parentAccountResolution.Error);
        }

        var existingCodes = await _ledgerAccounts.ListCodesByPrefixAsync(
            range.SearchPrefix,
            cancellationToken);
        var usedCodes = existingCodes
            .Select(OnlyDigits)
            .Where(code => code.Length == range.CodeLength)
            .ToHashSet(StringComparer.Ordinal);

        var bounds = GetSuggestionBounds(range, parentAccountResolution.ParentAccount);
        var start = bounds.Start;
        var end = bounds.End;

        for (var value = start; value <= end; value++)
        {
            var candidate = value.ToString().PadLeft(range.CodeLength, '0');

            if (!IsInsideRange(candidate, range) || usedCodes.Contains(candidate))
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
                range.ParentCode,
                parentAccountResolution.ParentAccount?.Id.Value,
                parentAccountResolution.ParentAccount?.Code.Value,
                parentAccountResolution.ParentAccount?.Name));
        }

        return Result<SuggestLedgerAccountCodeResult>.Failure(ApplicationError.Conflict(
            nameof(query.Role),
            $"No ledger account code is available for role {range.Role}."));
    }

    private async Task<ParentAccountResolution> ResolveParentAccountAsync(
        Guid? parentAccountId,
        AccountCodeRange range,
        CancellationToken cancellationToken)
    {
        if (!parentAccountId.HasValue)
        {
            return new ParentAccountResolution(null, null);
        }

        var parentAccount = await _ledgerAccounts.GetByIdAsync(
            LedgerAccountId.Create(parentAccountId.Value),
            cancellationToken);

        if (parentAccount is null)
        {
            return new ParentAccountResolution(null, ApplicationError.NotFound(
                nameof(SuggestLedgerAccountCodeQuery.ParentAccountId),
                "Parent ledger account was not found."));
        }

        var configuredParentCode = range.ParentCode?.Trim() ?? "";

        if (configuredParentCode != ""
            && !LedgerAccountHierarchyPolicy.IsParentInsideRangeFamily(range, parentAccount.Code.Value))
        {
            return new ParentAccountResolution(null, ApplicationError.Validation(
                nameof(SuggestLedgerAccountCodeQuery.ParentAccountId),
                $"Parent account must be {configuredParentCode} or one of its descendants for range {range.DisplayName}."));
        }

        if (configuredParentCode == ""
            && !range.IsPostingAccount
            && (!LedgerAccountHierarchyPolicy.IsStructuralLevel(parentAccount.Level)
                || parentAccount.IsPostingAccount))
        {
            return new ParentAccountResolution(null, ApplicationError.Validation(
                nameof(SuggestLedgerAccountCodeQuery.ParentAccountId),
                "Structural account suggestions require a non-posting structural parent account."));
        }

        if (parentAccount.Status != LedgerAccountStatus.Active)
        {
            return new ParentAccountResolution(null, ApplicationError.Validation(
                nameof(SuggestLedgerAccountCodeQuery.ParentAccountId),
                "Parent account must be active before adding child accounts."));
        }

        if (parentAccount.Type != range.AccountType || parentAccount.NormalBalance != range.NormalBalance)
        {
            return new ParentAccountResolution(null, ApplicationError.Validation(
                nameof(SuggestLedgerAccountCodeQuery.ParentAccountId),
                $"Parent account must use {range.AccountType} / {range.NormalBalance} for range {range.DisplayName}."));
        }

        return new ParentAccountResolution(parentAccount, null);
    }

    private static SuggestionBounds GetSuggestionBounds(
        AccountCodeRange range,
        LedgerAccount? parentAccount)
    {
        var bounds = LedgerAccountHierarchyPolicy.GetSuggestionBounds(range, parentAccount);

        return new SuggestionBounds(bounds.Start, bounds.End);
    }

    private static bool IsInsideRange(string code, AccountCodeRange range)
    {
        return code.Length == range.CodeLength
            && code.StartsWith(range.SearchPrefix, StringComparison.Ordinal)
            && StringComparer.Ordinal.Compare(code, range.RangeStart) >= 0
            && StringComparer.Ordinal.Compare(code, range.RangeEnd) <= 0;
    }

    private static string OnlyDigits(string value)
    {
        return new string(value.Where(char.IsDigit).ToArray());
    }

    private sealed record ParentAccountResolution(
        LedgerAccount? ParentAccount,
        ApplicationError? Error);

    private sealed record SuggestionBounds(
        BigInteger Start,
        BigInteger End);
}
