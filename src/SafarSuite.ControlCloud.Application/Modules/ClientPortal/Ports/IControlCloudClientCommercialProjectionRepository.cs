using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

public interface IControlCloudClientCommercialProjectionRepository
{
    Task<ControlCloudClientCommercialProjection?> GetByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        ControlCloudClientCommercialProjection projection,
        CancellationToken cancellationToken = default);
}
