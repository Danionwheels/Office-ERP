using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class FileControlCloudClientCommercialProjectionRepository
    : IControlCloudClientCommercialProjectionRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileControlCloudClientCommercialProjectionRepository(ControlCloudReceiverOptions options)
    {
        _storePath = ResolveStorePath(options.ProjectionStorePath);
    }

    public async Task<ControlCloudClientCommercialProjection?> GetByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var projections = await ReadAllAsync(cancellationToken);

            return projections.TryGetValue(clientId, out var projection)
                ? projection
                : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(
        ControlCloudClientCommercialProjection projection,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var projections = await ReadAllAsync(cancellationToken);
            projections[projection.ClientId] = projection;

            await WriteAllAsync(projections, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Dictionary<Guid, ControlCloudClientCommercialProjection>> ReadAllAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_storePath))
        {
            return new Dictionary<Guid, ControlCloudClientCommercialProjection>();
        }

        await using var stream = new FileStream(
            _storePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);
        var projections = await JsonSerializer.DeserializeAsync<IReadOnlyCollection<ControlCloudClientCommercialProjection>>(
            stream,
            JsonOptions,
            cancellationToken);

        return (projections ?? Array.Empty<ControlCloudClientCommercialProjection>())
            .GroupBy(projection => projection.ClientId)
            .ToDictionary(group => group.Key, group => group.Last());
    }

    private async Task WriteAllAsync(
        IReadOnlyDictionary<Guid, ControlCloudClientCommercialProjection> projections,
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
            projections.Values.OrderBy(projection => projection.ClientId).ToArray(),
            JsonOptions,
            cancellationToken);
    }

    private static string ResolveStorePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "App_Data/control-cloud-client-projections.json"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}
