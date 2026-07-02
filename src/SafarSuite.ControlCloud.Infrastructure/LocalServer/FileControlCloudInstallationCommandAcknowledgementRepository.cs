using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Infrastructure.LocalServer;

public sealed class FileControlCloudInstallationCommandAcknowledgementRepository
    : IControlCloudInstallationCommandAcknowledgementRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileControlCloudInstallationCommandAcknowledgementRepository(
        ControlCloudCommandQueueOptions options)
    {
        _storePath = ResolveStorePath(options.AcknowledgementStorePath);
    }

    public async Task AddAsync(
        ControlCloudInstallationCommandAcknowledgement acknowledgement,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var acknowledgements = await ReadAllAsync(cancellationToken);
            acknowledgements.Add(acknowledgement);

            await WriteAllAsync(acknowledgements, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<ControlCloudInstallationCommandAcknowledgement>> ReadAllAsync(
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
        var acknowledgements =
            await JsonSerializer.DeserializeAsync<List<ControlCloudInstallationCommandAcknowledgement>>(
                stream,
                JsonOptions,
                cancellationToken);

        return acknowledgements ?? [];
    }

    private async Task WriteAllAsync(
        IReadOnlyCollection<ControlCloudInstallationCommandAcknowledgement> acknowledgements,
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
            acknowledgements
                .OrderBy(acknowledgement => acknowledgement.AcknowledgedAtUtc)
                .ThenBy(acknowledgement => acknowledgement.AcknowledgementId)
                .ToArray(),
            JsonOptions,
            cancellationToken);
    }

    private static string ResolveStorePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "App_Data/control-cloud-installation-command-acknowledgements.json"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}
