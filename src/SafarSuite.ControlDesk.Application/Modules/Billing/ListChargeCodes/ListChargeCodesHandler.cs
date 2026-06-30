using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.ListChargeCodes;

public sealed class ListChargeCodesHandler
{
    private readonly IChargeCodeRepository _chargeCodes;

    public ListChargeCodesHandler(IChargeCodeRepository chargeCodes)
    {
        _chargeCodes = chargeCodes;
    }

    public async Task<Result<ListChargeCodesResult>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var chargeCodes = await _chargeCodes.ListAsync(cancellationToken);

        return Result<ListChargeCodesResult>.Success(new ListChargeCodesResult(
            chargeCodes.Select(chargeCode => new ChargeCodeLookupResult(
                chargeCode.Id.Value,
                chargeCode.Code.Value,
                chargeCode.Name,
                chargeCode.DefaultUnitPrice.Amount,
                chargeCode.DefaultUnitPrice.CurrencyCode,
                chargeCode.RevenueAccountId.Value,
                chargeCode.TaxAccountId?.Value,
                chargeCode.Status.ToString())).ToArray()));
    }
}
