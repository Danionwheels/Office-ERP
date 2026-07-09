using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Heartbeats.Ports;
using SafarSuite.LocalServer.Application.Pairing.Ports;
using SafarSuite.LocalServer.Domain.Pairing;

namespace SafarSuite.LocalServer.Api.Modules.LocalServer;

public sealed class LocalServerHeartbeatPairingStatusProvider
    : ILocalServerHeartbeatPairingStatusProvider
{
    private const string FirstManagerAssignedRole = "FirstManagerDevice";

    private readonly ILocalServerDevicePairingStore _pairingStore;
    private readonly LocalServerPairingOptions _pairingOptions;

    public LocalServerHeartbeatPairingStatusProvider(
        ILocalServerDevicePairingStore pairingStore,
        LocalServerPairingOptions pairingOptions)
    {
        _pairingStore = pairingStore;
        _pairingOptions = pairingOptions;
    }

    public async Task<LocalServerPairingStatusResponse?> GetCurrentAsync(
        Guid clientId,
        string installationId,
        CancellationToken cancellationToken = default)
    {
        var cleanInstallationId = installationId.Trim();

        if (clientId == Guid.Empty || string.IsNullOrWhiteSpace(cleanInstallationId))
        {
            return null;
        }

        var devices = (await _pairingStore.ListAsync(cancellationToken))
            .Where(device =>
                device.ClientId == clientId
                && string.Equals(
                    device.InstallationId,
                    cleanInstallationId,
                    StringComparison.Ordinal))
            .ToArray();
        var firstManagerDevice = devices
            .Where(device =>
                string.Equals(
                    device.AssignedRole,
                    FirstManagerAssignedRole,
                    StringComparison.Ordinal)
                && string.Equals(
                    device.DeviceStatus,
                    LocalServerDevicePairingRecordStatuses.Approved,
                    StringComparison.Ordinal))
            .OrderBy(device => device.ApprovedAtUtc ?? device.UpdatedAtUtc)
            .FirstOrDefault();
        var lastDeviceUpdatedAtUtc = devices.Length == 0
            ? (DateTimeOffset?)null
            : devices.Max(device => device.UpdatedAtUtc);

        return new LocalServerPairingStatusResponse(
            NormalizePairingMode(_pairingOptions.PairingMode),
            devices.Length,
            CountDevices(devices, LocalServerDevicePairingRecordStatuses.Pending),
            CountDevices(devices, LocalServerDevicePairingRecordStatuses.Approved),
            CountDevices(devices, LocalServerDevicePairingRecordStatuses.Suspended),
            CountDevices(devices, LocalServerDevicePairingRecordStatuses.Revoked),
            firstManagerDevice is not null,
            firstManagerDevice?.ApprovedAtUtc,
            lastDeviceUpdatedAtUtc);
    }

    private static int CountDevices(
        IEnumerable<LocalServerDevicePairingRecord> devices,
        string status)
    {
        return devices.Count(device =>
            string.Equals(device.DeviceStatus, status, StringComparison.Ordinal));
    }

    private static string NormalizePairingMode(string? pairingMode)
    {
        return string.IsNullOrWhiteSpace(pairingMode)
            ? LocalServerPairingModes.ManagerApproval
            : pairingMode.Trim();
    }
}
