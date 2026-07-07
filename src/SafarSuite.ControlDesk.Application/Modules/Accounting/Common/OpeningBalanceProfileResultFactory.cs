using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetOpeningBalanceProfile;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.Common;

public sealed class OpeningBalanceProfileResultFactory
{
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly IClock _clock;

    public OpeningBalanceProfileResultFactory(
        ILedgerAccountRepository ledgerAccounts,
        IClock clock)
    {
        _ledgerAccounts = ledgerAccounts;
        _clock = clock;
    }

    public async Task<GetOpeningBalanceProfileResult> CreateAsync(
        string companyCode,
        OpeningBalanceProfile? profile,
        CancellationToken cancellationToken = default)
    {
        if (profile is null)
        {
            var today = DateOnly.FromDateTime(_clock.UtcNow.DateTime);

            return new GetOpeningBalanceProfileResult(
                companyCode,
                new DateOnly(today.Year, 1, 1),
                new DateOnly(today.Year, 12, 31),
                OpeningBalanceProfileStatus.Open.ToString(),
                true,
                null,
                null,
                false,
                null,
                null);
        }

        var carryForwardAccount = await GetAccountAsync(
            profile.ProfitAndLossCarryForwardAccountId,
            cancellationToken);

        return new GetOpeningBalanceProfileResult(
            profile.CompanyCode,
            profile.FiscalYearFrom,
            profile.FiscalYearTo,
            profile.Status.ToString(),
            profile.TransactionsAllowed,
            profile.ProfitAndLossCarryForwardAccountId?.Value,
            carryForwardAccount,
            profile.IsConfigured,
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc);
    }

    private async Task<OpeningBalanceProfileAccountResult?> GetAccountAsync(
        LedgerAccountId? accountId,
        CancellationToken cancellationToken)
    {
        if (!accountId.HasValue)
        {
            return null;
        }

        var account = await _ledgerAccounts.GetByIdAsync(accountId.Value, cancellationToken);

        return account is null
            ? null
            : new OpeningBalanceProfileAccountResult(
                account.Id.Value,
                account.Code.Value,
                account.Name,
                account.Type.ToString(),
                account.NormalBalance.ToString(),
                account.Status.ToString());
    }
}
