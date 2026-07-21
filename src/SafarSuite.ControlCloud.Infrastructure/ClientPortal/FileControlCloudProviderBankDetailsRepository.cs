using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class FileControlCloudProviderBankDetailsRepository
    : IControlCloudProviderBankDetailsRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileControlCloudProviderBankDetailsRepository(ClientPortalAccessOptions options) =>
        _storePath = Resolve(options.ProviderBankDetailsStorePath);

    public async Task<ControlCloudProviderBankDetails?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            if (!File.Exists(_storePath))
            {
                return null;
            }

            await using var stream = File.OpenRead(_storePath);
            return await JsonSerializer.DeserializeAsync<ControlCloudProviderBankDetails>(
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
        ControlCloudProviderBankDetails bankDetails,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_storePath)!);
            await using var stream = new FileStream(
                _storePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read);
            await JsonSerializer.SerializeAsync(stream, bankDetails, JsonOptions, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string Resolve(string path) => Path.IsPathRooted(path)
        ? path
        : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
}
