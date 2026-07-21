using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.LocalServer.Application.Heartbeats.Ports;

public interface ILocalServerHeartbeatPairingStatusProvider
{
    Task<LocalServerPairingStatusResponse?> GetCurrentAsync(
        Guid clientId,
        string installationId,
        CancellationToken cancellationToken = default);
}
