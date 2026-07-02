using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.Audit.Ports;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class FileClientPortalAuditRecorder : IClientPortalAuditRecorder, IControlCloudAuditEventReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly bool _enabled;
    private readonly string _storePath;

    public FileClientPortalAuditRecorder(ClientPortalAuditOptions options)
    {
        _enabled = options.Enabled;
        _storePath = ResolveStorePath(options.StorePath);
    }

    public async Task RecordAsync(
        ClientPortalAuditRecord audit,
        CancellationToken cancellationToken = default)
    {
        if (!_enabled)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken);

        try
        {
            var directory = Path.GetDirectoryName(_storePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var line = JsonSerializer.Serialize(audit, JsonOptions);
            await File.AppendAllTextAsync(
                _storePath,
                line + Environment.NewLine,
                cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<ClientPortalAuditRecord>> ListAsync(
        Guid? clientId,
        string? eventType,
        int take,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            if (!_enabled || !File.Exists(_storePath))
            {
                return Array.Empty<ClientPortalAuditRecord>();
            }

            var lines = await File.ReadAllLinesAsync(_storePath, cancellationToken);
            var records = new List<ClientPortalAuditRecord>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                ClientPortalAuditRecord? record;

                try
                {
                    record = JsonSerializer.Deserialize<ClientPortalAuditRecord>(
                        line,
                        JsonOptions);
                }
                catch (JsonException)
                {
                    continue;
                }

                if (record is null)
                {
                    continue;
                }

                if (clientId.HasValue && record.ClientId != clientId)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(eventType)
                    && !string.Equals(record.EventType, eventType.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                records.Add(record);
            }

            return records
                .OrderByDescending(record => record.OccurredAtUtc)
                .Take(Math.Clamp(take, 1, 500))
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string ResolveStorePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "App_Data/client-portal-audit-events.jsonl"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}
