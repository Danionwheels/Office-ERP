using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

public interface IClientPortalPasswordResetRepository
{
    Task<ControlCloudClientPortalPasswordReset?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ControlCloudClientPortalPasswordReset passwordReset,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        ControlCloudClientPortalPasswordReset passwordReset,
        CancellationToken cancellationToken = default);
}
