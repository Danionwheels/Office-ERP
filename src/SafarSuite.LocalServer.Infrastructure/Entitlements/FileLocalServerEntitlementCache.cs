using System.Text.Json;
using SafarSuite.LocalServer.Application.Entitlements.Ports;
using SafarSuite.LocalServer.Domain.Entitlements;

namespace SafarSuite.LocalServer.Infrastructure.Entitlements;

public sealed class FileLocalServerEntitlementCache : ILocalServerEntitlementCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileLocalServerEntitlementCache(LocalServerEntitlementTrustOptions options)
    {
        _storePath = ResolveStorePath(options.CacheStorePath);
    }

    public async Task<LocalServerCachedEntitlement?> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            if (!File.Exists(_storePath))
            {
                return null;
            }

            await using var stream = new FileStream(
                _storePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

            return await JsonSerializer.DeserializeAsync<LocalServerCachedEntitlement>(
                stream,
                JsonOptions,
                cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(
        LocalServerCachedEntitlement entitlement,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
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
                entitlement,
                JsonOptions,
                cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string ResolveStorePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "App_Data/local-server-entitlement-cache.json"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}
