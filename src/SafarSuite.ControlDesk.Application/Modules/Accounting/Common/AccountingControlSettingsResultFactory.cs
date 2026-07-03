using SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingControlSettings;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.Common;

public sealed class AccountingControlSettingsResultFactory
{
    private const string DefaultBaseCurrencyCode = "PKR";

    private readonly ILedgerAccountRepository _ledgerAccounts;

    public AccountingControlSettingsResultFactory(ILedgerAccountRepository ledgerAccounts)
    {
        _ledgerAccounts = ledgerAccounts;
    }

    public async Task<GetAccountingControlSettingsResult> CreateAsync(
        string companyCode,
        AccountingControlSettings? settings,
        CancellationToken cancellationToken = default)
    {
        if (settings is null)
        {
            return new GetAccountingControlSettingsResult(
                companyCode,
                DefaultBaseCurrencyCode,
                null,
                null,
                null,
                null,
                null,
                null,
                false,
                null,
                null);
        }

        var retainedEarningsAccount = await GetAccountAsync(
            settings.RetainedEarningsAccountId,
            cancellationToken);
        var incomeSummaryAccount = await GetAccountAsync(
            settings.IncomeSummaryAccountId,
            cancellationToken);
        var roundingAccount = await GetAccountAsync(
            settings.RoundingAccountId,
            cancellationToken);

        return new GetAccountingControlSettingsResult(
            settings.CompanyCode,
            settings.BaseCurrencyCode,
            settings.RetainedEarningsAccountId?.Value,
            retainedEarningsAccount,
            settings.IncomeSummaryAccountId?.Value,
            incomeSummaryAccount,
            settings.RoundingAccountId?.Value,
            roundingAccount,
            settings.IsConfigured,
            settings.CreatedAtUtc,
            settings.UpdatedAtUtc);
    }

    private async Task<AccountingControlAccountResult?> GetAccountAsync(
        LedgerAccountId? accountId,
        CancellationToken cancellationToken)
    {
        if (!accountId.HasValue)
        {
            return null;
        }

        var account = await _ledgerAccounts.GetByIdAsync(accountId.Value, cancellationToken);

        return account is null ? null : ToAccountResult(account);
    }

    private static AccountingControlAccountResult ToAccountResult(LedgerAccount account)
    {
        return new AccountingControlAccountResult(
            account.Id.Value,
            account.Code.Value,
            account.Name,
            account.Type.ToString(),
            account.NormalBalance.ToString(),
            account.Status.ToString());
    }
}
