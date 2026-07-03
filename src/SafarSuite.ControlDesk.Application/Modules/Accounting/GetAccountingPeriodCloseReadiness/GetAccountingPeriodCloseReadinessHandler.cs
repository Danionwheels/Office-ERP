using SafarSuite.ControlDesk.Application.Common.Results;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingPeriodCloseReadiness;

public sealed class GetAccountingPeriodCloseReadinessHandler
{
    private readonly AccountingPeriodCloseReadinessService _readinessService;

    public GetAccountingPeriodCloseReadinessHandler(AccountingPeriodCloseReadinessService readinessService)
    {
        _readinessService = readinessService;
    }

    public async Task<Result<GetAccountingPeriodCloseReadinessResult>> HandleAsync(
        GetAccountingPeriodCloseReadinessQuery query,
        CancellationToken cancellationToken = default)
    {
        return await _readinessService.CheckAsync(query.AccountingPeriodId, cancellationToken);
    }
}
