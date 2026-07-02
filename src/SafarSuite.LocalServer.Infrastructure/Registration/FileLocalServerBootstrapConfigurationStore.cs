using System.Text.Json;
using SafarSuite.LocalServer.Application.Registration.Ports;
using SafarSuite.LocalServer.Domain.Registration;

namespace SafarSuite.LocalServer.Infrastructure.Registration;

public sealed class FileLocalServerBootstrapConfigurationStore
    : ILocalServerBootstrapConfigurationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileLocalServerBootstrapConfigurationStore(
        LocalServerBootstrapTrustOptions options)
    {
        _storePath = ResolveStorePath(options.ConfigurationStorePath);
    }

    public async Task<LocalServerBootstrapConfiguration?> GetCurrentAsync(
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

            return await JsonSerializer.DeserializeAsync<LocalServerBootstrapConfiguration>(
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
        LocalServerBootstrapConfiguration configuration,
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
                configuration,
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
            ? "App_Data/local-server-bootstrap-configuration.json"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}
