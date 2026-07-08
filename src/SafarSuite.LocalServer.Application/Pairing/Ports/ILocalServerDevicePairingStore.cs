using SafarSuite.LocalServer.Domain.Pairing;

namespace SafarSuite.LocalServer.Application.Pairing.Ports;

public interface ILocalServerDevicePairingStore
{
    Task<IReadOnlyCollection<LocalServerDevicePairingRecord>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<LocalServerDevicePairingRecord?> GetByPairingRequestIdAsync(
        Guid pairingRequestId,
        CancellationToken cancellationToken = default);

    Task<LocalServerDevicePairingRecord?> GetByDeviceIdAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        LocalServerDevicePairingRecord record,
        CancellationToken cancellationToken = default);
}
