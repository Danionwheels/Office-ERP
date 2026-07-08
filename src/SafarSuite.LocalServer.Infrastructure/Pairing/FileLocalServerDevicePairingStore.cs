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
            var nextState = new LocalServerDevicePairingStoreState(records);
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
                nextState,
                JsonOptions,
                cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
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

        return await JsonSerializer.DeserializeAsync<LocalServerDevicePairingStoreState>(
            stream,
            JsonOptions,
            cancellationToken)
            ?? LocalServerDevicePairingStoreState.Empty;
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
        IReadOnlyCollection<LocalServerDevicePairingRecord> Records)
    {
        public static LocalServerDevicePairingStoreState Empty { get; } = new([]);
    }
}
