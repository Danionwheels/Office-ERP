using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.Ports;

public interface IPortalPaymentClaimRepository
{
    Task AddAsync(PortalPaymentClaim claim, CancellationToken cancellationToken = default);

    Task<PortalPaymentClaim?> GetByIdAsync(
        PortalPaymentClaimId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PortalPaymentClaim>> ListAsync(
        ClientId? clientId = null,
        CancellationToken cancellationToken = default);
}
