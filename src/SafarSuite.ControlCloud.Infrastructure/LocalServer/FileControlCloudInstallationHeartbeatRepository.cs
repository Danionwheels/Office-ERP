using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Infrastructure.LocalServer;

public sealed class FileControlCloudInstallationHeartbeatRepository
    : IControlCloudInstallationHeartbeatRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileControlCloudInstallationHeartbeatRepository(
        ControlCloudCommandQueueOptions options)
    {
        _storePath = ResolveStorePath(options.HeartbeatStorePath);
    }

    public async Task AddAsync(
        ControlCloudInstallationHeartbeat heartbeat,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var heartbeats = await ReadAllAsync(cancellationToken);
            heartbeats.Add(heartbeat);

            await WriteAllAsync(heartbeats, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ControlCloudInstallationHeartbeat?> GetLatestByInstallationIdAsync(
        string installationId,
        CancellationToken cancellationToken = default)
    {
        var cleanInstallationId = installationId.Trim();

        await _gate.WaitAsync(cancellationToken);

        try
        {
            return (await ReadAllAsync(cancellationToken))
                .Where(heartbeat => heartbeat.InstallationId == cleanInstallationId)
                .OrderBy(heartbeat => heartbeat.ReceivedAtUtc)
                .ThenBy(heartbeat => heartbeat.HeartbeatId)
                .LastOrDefault();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<ControlCloudInstallationHeartbeat>> ReadAllAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_storePath))
        {
            return [];
        }

        await using var stream = new FileStream(
            _storePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);
        var heartbeats =
            await JsonSerializer.DeserializeAsync<List<ControlCloudInstallationHeartbeat>>(
                stream,
                JsonOptions,
                cancellationToken);

        return heartbeats ?? [];
    }

    private async Task WriteAllAsync(
        IReadOnlyCollection<ControlCloudInstallationHeartbeat> heartbeats,
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
            heartbeats
                .OrderBy(heartbeat => heartbeat.ReceivedAtUtc)
                .ThenBy(heartbeat => heartbeat.HeartbeatId)
                .ToArray(),
            JsonOptions,
            cancellationToken);
    }

    private static string ResolveStorePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "App_Data/control-cloud-installation-heartbeats.json"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}
