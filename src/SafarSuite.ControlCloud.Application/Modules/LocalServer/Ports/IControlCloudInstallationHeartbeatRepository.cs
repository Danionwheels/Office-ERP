using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;

public interface IControlCloudInstallationHeartbeatRepository
{
    Task AddAsync(
        ControlCloudInstallationHeartbeat heartbeat,
        CancellationToken cancellationToken = default);

    Task<ControlCloudInstallationHeartbeat?> GetLatestByInstallationIdAsync(
        string installationId,
        CancellationToken cancellationToken = default);
}
