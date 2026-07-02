using System.Text.Json;
using SafarSuite.LocalServer.Application.Entitlements.Ports;
using SafarSuite.LocalServer.Domain.Entitlements;

namespace SafarSuite.LocalServer.Infrastructure.Entitlements;

public sealed class FileLocalServerEntitlementTrustStateStore
    : ILocalServerEntitlementTrustStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileLocalServerEntitlementTrustStateStore(
        LocalServerEntitlementTrustOptions options)
    {
        _storePath = ResolveStorePath(options.TrustStateStorePath);
    }

    public async Task<LocalServerEntitlementTrustState?> GetAsync(
        string installationId,
        CancellationToken cancellationToken = default)
    {
        var cleanInstallationId = installationId.Trim();

        if (cleanInstallationId.Length == 0)
        {
            return null;
        }

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

            var state = await JsonSerializer.DeserializeAsync<LocalServerEntitlementTrustState>(
                stream,
                JsonOptions,
                cancellationToken);

            return state is not null
                && string.Equals(state.InstallationId, cleanInstallationId, StringComparison.Ordinal)
                    ? state
                    : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(
        LocalServerEntitlementTrustState state,
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
                state,
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
            ? "App_Data/local-server-entitlement-trust-state.json"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}
