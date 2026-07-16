using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.GetPortalBankDetails;

public sealed class GetClientPortalBankDetailsHandler
{
    private readonly IControlCloudProviderBankDetailsRepository _bankDetails;

    public GetClientPortalBankDetailsHandler(IControlCloudProviderBankDetailsRepository bankDetails) =>
        _bankDetails = bankDetails;

    public async Task<ControlCloudProviderBankDetails> HandleAsync(
        CancellationToken cancellationToken = default) =>
        await _bankDetails.GetAsync(cancellationToken)
        ?? new ControlCloudProviderBankDetails("", "", "", "", "", DateTimeOffset.MinValue);
}
