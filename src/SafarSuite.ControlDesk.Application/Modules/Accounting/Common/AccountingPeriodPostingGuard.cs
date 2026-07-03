using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.Common;

public sealed class AccountingPeriodPostingGuard
{
    private readonly IAccountingPeriodRepository _periods;

    public AccountingPeriodPostingGuard(IAccountingPeriodRepository periods)
    {
        _periods = periods;
    }

    public async Task<ApplicationError?> ValidateOpenPeriodAsync(
        DateOnly postingDate,
        string target,
        string? companyCode = null,
        CancellationToken cancellationToken = default)
    {
        var companyError = AccountingSetupDefaults.ValidateSingleCompanyCode(companyCode, target);

        if (companyError is not null)
        {
            return companyError;
        }

        var normalizedCompanyCode = AccountingSetupDefaults.NormalizeCompanyCode(companyCode);
        var period = await _periods.GetContainingDateAsync(
            normalizedCompanyCode,
            postingDate,
            cancellationToken);

        if (period is null)
        {
            var configuredPeriods = await _periods.ListByCompanyAsync(
                normalizedCompanyCode,
                cancellationToken: cancellationToken);

            return configuredPeriods.Count == 0
                ? null
                : ApplicationError.Validation(
                    target,
                    $"No accounting period contains posting date {postingDate:yyyy-MM-dd}.");
        }

        return period.Status == AccountingPeriodStatus.Closed
            ? ApplicationError.Conflict(
                target,
                $"Accounting period {period.Name} is closed.")
            : null;
    }
}
