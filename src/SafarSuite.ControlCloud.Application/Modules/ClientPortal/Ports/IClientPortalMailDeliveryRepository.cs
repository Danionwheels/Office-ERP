using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

public interface IClientPortalMailDeliveryRepository
{
    Task AddAsync(
        ControlCloudClientPortalMailDelivery delivery,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ControlCloudClientPortalMailDelivery>> ClaimDueAsync(
        DateTimeOffset nowUtc,
        TimeSpan leaseDuration,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        ControlCloudClientPortalMailDelivery delivery,
        Guid expectedLeaseId,
        CancellationToken cancellationToken = default);
}
