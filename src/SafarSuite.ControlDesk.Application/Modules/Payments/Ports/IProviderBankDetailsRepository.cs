using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.Ports;

public interface IProviderBankDetailsRepository
{
    Task<ProviderBankDetails?> GetAsync(CancellationToken cancellationToken = default);

    Task AddAsync(ProviderBankDetails details, CancellationToken cancellationToken = default);
}
