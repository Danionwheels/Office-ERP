using System.Text.Json;
using SafarSuite.LocalServer.Application.Commands.Ports;
using SafarSuite.LocalServer.Domain.Commands;

namespace SafarSuite.LocalServer.Infrastructure.Commands;

public sealed class FileLocalServerAppActivationRevocationStore
    : ILocalServerAppActivationRevocationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileLocalServerAppActivationRevocationStore(
        LocalServerCommandOptions options)
    {
        _storePath = ResolveStorePath(options.AppActivationRevocationStorePath);
    }

    public async Task SaveAsync(
        LocalServerAppActivationRevocationRecord record,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var records = await ReadAllUnsafeAsync(cancellationToken);
            records[record.ActivationIssueId] = record;

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
                records
                    .Values
                    .OrderBy(value => value.RecordedAtUtc)
                    .ThenBy(value => value.ActivationIssueId)
                    .ToArray(),
                JsonOptions,
                cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<LocalServerAppActivationRevocationRecord?> GetByActivationIssueIdAsync(
        Guid activationIssueId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var records = await ReadAllUnsafeAsync(cancellationToken);

            return records.TryGetValue(activationIssueId, out var record)
                ? record
                : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Dictionary<Guid, LocalServerAppActivationRevocationRecord>> ReadAllUnsafeAsync(
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

        var records = await JsonSerializer.DeserializeAsync<IReadOnlyCollection<LocalServerAppActivationRevocationRecord>>(
            stream,
            JsonOptions,
            cancellationToken);

        return (records ?? Array.Empty<LocalServerAppActivationRevocationRecord>())
            .GroupBy(record => record.ActivationIssueId)
            .ToDictionary(group => group.Key, group => group.Last());
    }

    private static string ResolveStorePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "App_Data/local-server-app-activation-revocations.json"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}
