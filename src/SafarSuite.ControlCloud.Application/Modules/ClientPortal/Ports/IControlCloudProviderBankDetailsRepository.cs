using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

public interface IControlCloudProviderBankDetailsRepository
{
    Task<ControlCloudProviderBankDetails?> GetAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        ControlCloudProviderBankDetails bankDetails,
        CancellationToken cancellationToken = default);
}
