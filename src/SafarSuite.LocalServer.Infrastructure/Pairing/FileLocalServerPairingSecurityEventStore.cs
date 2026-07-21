using System.Text.Json;
using SafarSuite.LocalServer.Application.Pairing.Ports;
using SafarSuite.LocalServer.Domain.Pairing;

namespace SafarSuite.LocalServer.Infrastructure.Pairing;

public sealed class FileLocalServerPairingSecurityEventStore
    : ILocalServerPairingSecurityEventStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly LocalServerPairingAbuseControlOptions _options;
    private readonly string _storePath;

    public FileLocalServerPairingSecurityEventStore(
        LocalServerPairingAbuseControlOptions options)
    {
        _options = options;
        _storePath = ResolveStorePath(options.SecurityEventStorePath);
    }

    public async Task<IReadOnlyCollection<LocalServerPairingSecurityEvent>> ListEventsAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var state = await ReadStateAsync(cancellationToken);
            var readLimit = Math.Clamp(take, 1, Math.Clamp(_options.SecurityEventReadLimit, 1, 1000));

            return state.Events
                .OrderByDescending(record => record.OccurredAtUtc)
                .ThenByDescending(record => record.EventId)
                .Take(readLimit)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RecordEventAsync(
        LocalServerPairingSecurityEvent record,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var state = await ReadStateAsync(cancellationToken);
            var retentionDays = Math.Clamp(_options.SecurityEventRetentionDays, 1, 3650);
            var maxRecords = Math.Clamp(_options.SecurityEventMaxRecords, 100, 100000);
            var cutoffUtc = record.OccurredAtUtc.AddDays(-retentionDays);
            var events = state.Events
                .Where(existing => existing.OccurredAtUtc >= cutoffUtc
                    && existing.EventId != record.EventId)
                .Append(record)
                .OrderByDescending(existing => existing.OccurredAtUtc)
                .ThenByDescending(existing => existing.EventId)
                .Take(maxRecords)
                .OrderBy(existing => existing.OccurredAtUtc)
                .ThenBy(existing => existing.EventId)
                .ToArray();
            var nextState = state with
            {
                Events = events
            };

            await WriteStateAsync(nextState, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyCollection<LocalServerPairingAbuseSourceDecision>> ListSourceDecisionsAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var state = await ReadStateAsync(cancellationToken);

            return state.SourceDecisions
                .OrderByDescending(record => record.CreatedAtUtc)
                .ThenBy(record => record.SourceKey, StringComparer.Ordinal)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<LocalServerPairingAbuseSourceDecision?> GetActiveSourceDecisionAsync(
        string sourceKey,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var state = await ReadStateAsync(cancellationToken);
            var latestDecision = state.SourceDecisions
                .Where(record => string.Equals(record.SourceKey, sourceKey, StringComparison.Ordinal))
                .OrderByDescending(record => record.CreatedAtUtc)
                .FirstOrDefault();

            if (latestDecision is null
                || !IsRestrictiveAction(latestDecision.Action)
                || (latestDecision.ExpiresAtUtc is not null && latestDecision.ExpiresAtUtc <= asOfUtc))
            {
                return null;
            }

            return latestDecision;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveSourceDecisionAsync(
        LocalServerPairingAbuseSourceDecision decision,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var state = await ReadStateAsync(cancellationToken);
            var sourceDecisions = state.SourceDecisions
                .Append(decision)
                .OrderBy(record => record.CreatedAtUtc)
                .ThenBy(record => record.SourceKey, StringComparer.Ordinal)
                .ToArray();
            var nextState = state with
            {
                SourceDecisions = sourceDecisions
            };

            await WriteStateAsync(nextState, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task WriteStateAsync(
        LocalServerPairingSecurityEventStoreState state,
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
            state,
            JsonOptions,
            cancellationToken);
    }

    private async Task<LocalServerPairingSecurityEventStoreState> ReadStateAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_storePath))
        {
            return LocalServerPairingSecurityEventStoreState.Empty;
        }

        await using var stream = new FileStream(
            _storePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);

        var state = await JsonSerializer.DeserializeAsync<LocalServerPairingSecurityEventStoreState>(
            stream,
            JsonOptions,
            cancellationToken)
            ?? LocalServerPairingSecurityEventStoreState.Empty;

        return new LocalServerPairingSecurityEventStoreState(
            state.Events ?? [],
            state.SourceDecisions ?? []);
    }

    private static string ResolveStorePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "App_Data/local-server-pairing-security-events.json"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }

    private static bool IsRestrictiveAction(string action)
    {
        return string.Equals(action, "Quarantine", StringComparison.Ordinal)
            || string.Equals(action, "Deny", StringComparison.Ordinal);
    }

    private sealed record LocalServerPairingSecurityEventStoreState(
        IReadOnlyCollection<LocalServerPairingSecurityEvent> Events,
        IReadOnlyCollection<LocalServerPairingAbuseSourceDecision> SourceDecisions)
    {
        public static LocalServerPairingSecurityEventStoreState Empty { get; } = new([], []);
    }
}
