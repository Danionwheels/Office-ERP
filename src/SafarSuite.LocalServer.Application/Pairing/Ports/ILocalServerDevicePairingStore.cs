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

    Task<LocalServerFirstManagerSetupTokenConsumptionRecord?> GetFirstManagerSetupTokenConsumptionAsync(
        Guid tokenId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        LocalServerDevicePairingRecord record,
        CancellationToken cancellationToken = default);

    Task SaveFirstManagerSetupTokenConsumptionAsync(
        LocalServerFirstManagerSetupTokenConsumptionRecord record,
        CancellationToken cancellationToken = default);

    Task<LocalServerDevicePairingStoreWriteResult> SaveDeviceAndFirstManagerSetupTokenConsumptionAsync(
        LocalServerDevicePairingRecord device,
        LocalServerFirstManagerSetupTokenConsumptionRecord consumption,
        CancellationToken cancellationToken = default);
}

public sealed record LocalServerDevicePairingStoreWriteResult(
    bool Succeeded,
    string? FailureCode,
    string? Detail)
{
    public static LocalServerDevicePairingStoreWriteResult Success()
    {
        return new LocalServerDevicePairingStoreWriteResult(
            true,
            null,
            null);
    }

    public static LocalServerDevicePairingStoreWriteResult Failure(
        string failureCode,
        string detail)
    {
        return new LocalServerDevicePairingStoreWriteResult(
            false,
            failureCode,
            detail);
    }
}
