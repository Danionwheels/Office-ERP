using System.Text.Json;
using SafarSuite.LocalServer.Application.Entitlements.Ports;
using SafarSuite.LocalServer.Domain.Entitlements;

namespace SafarSuite.LocalServer.Infrastructure.Entitlements;

public sealed class FileLocalServerEntitlementImportAuditStore
    : ILocalServerEntitlementImportAuditStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly int _maxRecords;
    private readonly string _storePath;

    public FileLocalServerEntitlementImportAuditStore(
        LocalServerEntitlementTrustOptions options)
    {
        _storePath = ResolveStorePath(options.ImportAuditStorePath);
        _maxRecords = Math.Max(1, options.MaxImportAuditRecords);
    }

    public async Task AppendAsync(
        LocalServerEntitlementImportAuditRecord record,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var records = await ReadRecordsUnsafeAsync(cancellationToken);
            records.Add(record);

            var trimmedRecords = records
                .OrderByDescending(existingRecord => existingRecord.OccurredAtUtc)
                .ThenByDescending(existingRecord => existingRecord.AuditRecordId)
                .Take(_maxRecords)
                .OrderBy(existingRecord => existingRecord.OccurredAtUtc)
                .ThenBy(existingRecord => existingRecord.AuditRecordId)
                .ToArray();

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
                trimmedRecords,
                JsonOptions,
                cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyCollection<LocalServerEntitlementImportAuditRecord>> GetRecentAsync(
        string installationId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var cleanInstallationId = installationId.Trim();

        if (cleanInstallationId.Length == 0 || take <= 0)
        {
            return Array.Empty<LocalServerEntitlementImportAuditRecord>();
        }

        await _gate.WaitAsync(cancellationToken);

        try
        {
            var records = await ReadRecordsUnsafeAsync(cancellationToken);

            return records
                .Where(record => string.Equals(
                    record.InstallationId,
                    cleanInstallationId,
                    StringComparison.Ordinal))
                .OrderByDescending(record => record.OccurredAtUtc)
                .ThenByDescending(record => record.AuditRecordId)
                .Take(Math.Min(take, 100))
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<LocalServerEntitlementImportAuditRecord>> ReadRecordsUnsafeAsync(
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

        return await JsonSerializer.DeserializeAsync<List<LocalServerEntitlementImportAuditRecord>>(
            stream,
            JsonOptions,
            cancellationToken) ?? [];
    }

    private static string ResolveStorePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "App_Data/local-server-entitlement-import-audit.json"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}
