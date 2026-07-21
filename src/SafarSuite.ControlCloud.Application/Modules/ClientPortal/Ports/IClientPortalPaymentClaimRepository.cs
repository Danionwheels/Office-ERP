using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

public interface IClientPortalPaymentClaimRepository
{
    Task<ControlCloudClientPortalPaymentClaim?> GetByIdAsync(
        Guid claimId,
        CancellationToken cancellationToken = default);

    Task<ControlCloudClientPortalPaymentClaim?> GetByClientAndReferenceAsync(
        Guid clientId,
        string normalizedTransferReferenceNumber,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ControlCloudClientPortalPaymentClaim>> ListAsync(
        Guid? clientId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ControlCloudClientPortalPaymentClaim claim,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        ControlCloudClientPortalPaymentClaim claim,
        CancellationToken cancellationToken = default);
}
