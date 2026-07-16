using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Payments.Common;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.GetProviderBankDetails;

public sealed class GetProviderBankDetailsHandler
{
    private readonly IProviderBankDetailsRepository _bankDetails;

    public GetProviderBankDetailsHandler(IProviderBankDetailsRepository bankDetails)
    {
        _bankDetails = bankDetails;
    }

    public async Task<Result<ProviderBankDetailsResult>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var details = await _bankDetails.GetAsync(cancellationToken);

        return Result<ProviderBankDetailsResult>.Success(ProviderBankDetailsResult.From(details));
    }
}
