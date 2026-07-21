using System.Text.Json;
using SafarSuite.LocalServer.Application.Pairing.Ports;
using SafarSuite.LocalServer.Domain.Pairing;

namespace SafarSuite.LocalServer.Infrastructure.Pairing;

public sealed class FileLocalServerDevicePairingStore
    : ILocalServerDevicePairingStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileLocalServerDevicePairingStore(
        LocalServerPairingStoreOptions options)
    {
        _storePath = ResolveStorePath(options.DeviceStorePath);
    }

    public async Task<IReadOnlyCollection<LocalServerDevicePairingRecord>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var state = await ReadStateAsync(cancellationToken);

            return state.Records
                .OrderByDescending(record => record.UpdatedAtUtc)
                .ThenByDescending(record => record.RequestedAtUtc)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<LocalServerDevicePairingRecord?> GetByPairingRequestIdAsync(
        Guid pairingRequestId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var state = await ReadStateAsync(cancellationToken);

            return state.Records.FirstOrDefault(
                record => record.PairingRequestId == pairingRequestId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<LocalServerDevicePairingRecord?> GetByDeviceIdAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var state = await ReadStateAsync(cancellationToken);

            return state.Records.FirstOrDefault(
                record => record.DeviceId == deviceId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<LocalServerFirstManagerSetupTokenConsumptionRecord?> GetFirstManagerSetupTokenConsumptionAsync(
        Guid tokenId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var state = await ReadStateAsync(cancellationToken);

            return state.ConsumedFirstManagerSetupTokens.FirstOrDefault(
                record => record.TokenId == tokenId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(
        LocalServerDevicePairingRecord record,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var state = await ReadStateAsync(cancellationToken);
            var records = state.Records
                .Where(existing => existing.PairingRequestId != record.PairingRequestId
                    && existing.DeviceId != record.DeviceId)
                .Append(record)
                .OrderBy(item => item.RequestedAtUtc)
                .ToArray();
            var nextState = state with
            {
                Records = records
            };

            await WriteStateAsync(nextState, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveFirstManagerSetupTokenConsumptionAsync(
        LocalServerFirstManagerSetupTokenConsumptionRecord record,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var state = await ReadStateAsync(cancellationToken);
            var tokenRecords = state.ConsumedFirstManagerSetupTokens
                .Where(existing => existing.TokenId != record.TokenId)
                .Append(record)
                .OrderBy(item => item.ConsumedAtUtc)
                .ToArray();
            var nextState = state with
            {
                ConsumedFirstManagerSetupTokens = tokenRecords
            };

            await WriteStateAsync(nextState, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<LocalServerDevicePairingStoreWriteResult> SaveDeviceAndFirstManagerSetupTokenConsumptionAsync(
        LocalServerDevicePairingRecord device,
        LocalServerFirstManagerSetupTokenConsumptionRecord consumption,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var state = await ReadStateAsync(cancellationToken);

            if (state.ConsumedFirstManagerSetupTokens.Any(existing => existing.TokenId == consumption.TokenId))
            {
                return LocalServerDevicePairingStoreWriteResult.Failure(
                    "FirstManagerSetupTokenAlreadyConsumed",
                    "First-manager setup token has already been consumed by this LocalServer.");
            }

            var currentDevice = state.Records.FirstOrDefault(
                existing => existing.PairingRequestId == device.PairingRequestId
                    && existing.DeviceId == device.DeviceId);

            if (currentDevice is null)
            {
                return LocalServerDevicePairingStoreWriteResult.Failure(
                    "PairingRequestNotFound",
                    "First-manager setup token references a pairing request that was not found.");
            }

            if (currentDevice.ClientId != consumption.ClientId
                || !string.Equals(currentDevice.InstallationId, consumption.InstallationId, StringComparison.Ordinal)
                || currentDevice.PairingRequestId != consumption.PairingRequestId
                || currentDevice.DeviceId != consumption.DeviceId)
            {
                return LocalServerDevicePairingStoreWriteResult.Failure(
                    "PairingRequestMismatch",
                    "First-manager setup token references a pairing request for another client, installation, or device.");
            }

            if (!string.Equals(
                    currentDevice.PairingRequestStatus,
                    LocalServerDevicePairingRecordStatuses.Pending,
                    StringComparison.Ordinal)
                || !string.Equals(
                    currentDevice.DeviceStatus,
                    LocalServerDevicePairingRecordStatuses.Pending,
                    StringComparison.Ordinal))
            {
                return LocalServerDevicePairingStoreWriteResult.Failure(
                    "DeviceStatusInvalid",
                    "First-manager setup token can only approve a pending device pairing request.");
            }

            var records = state.Records
                .Where(existing => existing.PairingRequestId != device.PairingRequestId
                    && existing.DeviceId != device.DeviceId)
                .Append(device)
                .OrderBy(item => item.RequestedAtUtc)
                .ToArray();
            var tokenRecords = state.ConsumedFirstManagerSetupTokens
                .Append(consumption)
                .OrderBy(item => item.ConsumedAtUtc)
                .ToArray();
            var nextState = state with
            {
                Records = records,
                ConsumedFirstManagerSetupTokens = tokenRecords
            };

            await WriteStateAsync(nextState, cancellationToken);

            return LocalServerDevicePairingStoreWriteResult.Success();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task WriteStateAsync(
        LocalServerDevicePairingStoreState state,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_storePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = new FileStream(
            _storePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read);
        await JsonSerializer.SerializeAsync(
            stream,
            state,
            JsonOptions,
            cancellationToken);
    }

    private async Task<LocalServerDevicePairingStoreState> ReadStateAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_storePath))
        {
            return LocalServerDevicePairingStoreState.Empty;
        }

        await using var stream = new FileStream(
            _storePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);

        var state = await JsonSerializer.DeserializeAsync<LocalServerDevicePairingStoreState>(
            stream,
            JsonOptions,
            cancellationToken)
            ?? LocalServerDevicePairingStoreState.Empty;

        return new LocalServerDevicePairingStoreState(
            state.Records ?? [],
            state.ConsumedFirstManagerSetupTokens ?? []);
    }

    private static string ResolveStorePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "App_Data/local-server-device-pairings.json"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }

    private sealed record LocalServerDevicePairingStoreState(
        IReadOnlyCollection<LocalServerDevicePairingRecord> Records,
        IReadOnlyCollection<LocalServerFirstManagerSetupTokenConsumptionRecord> ConsumedFirstManagerSetupTokens)
    {
        public static LocalServerDevicePairingStoreState Empty { get; } = new([], []);
    }
}
