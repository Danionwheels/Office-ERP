using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

public interface IControlCloudClientInstallationRepository
{
    Task<ControlCloudClientInstallation?> GetByInstallationIdAsync(
        string installationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ControlCloudClientInstallation installation,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        ControlCloudClientInstallation installation,
        CancellationToken cancellationToken = default);
}
