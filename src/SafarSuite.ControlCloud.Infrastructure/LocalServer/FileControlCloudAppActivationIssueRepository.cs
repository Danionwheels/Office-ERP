using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Infrastructure.LocalServer;

public sealed class FileControlCloudAppActivationIssueRepository
    : IControlCloudAppActivationIssueRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileControlCloudAppActivationIssueRepository(
        ControlCloudAppActivationSigningOptions options)
    {
        _storePath = ResolveStorePath(options.IssueStorePath);
    }

    public async Task AddAsync(
        ControlCloudAppActivationIssue issue,
        CancellationToken cancellationToken = default)
    {
        await SaveAsync(issue, cancellationToken);
    }

    public async Task<ControlCloudAppActivationIssue?> GetByIdAsync(
        Guid activationIssueId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var issues = await ReadAllAsync(cancellationToken);

            return issues.TryGetValue(activationIssueId, out var issue)
                ? issue
                : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(
        ControlCloudAppActivationIssue issue,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var issues = await ReadAllAsync(cancellationToken);
            issues[issue.ActivationIssueId] = issue;

            await WriteAllAsync(issues.Values, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<ControlCloudAppActivationIssue>> ListAsync(
        Guid clientId,
        string? installationId,
        Guid? appServerInstallationId,
        string? query,
        int take,
        CancellationToken cancellationToken = default)
    {
        var cleanInstallationId = NormalizeOptionalText(installationId);
        var cleanQuery = NormalizeOptionalText(query);
        var boundedTake = Math.Clamp(take, 1, 500);

        await _gate.WaitAsync(cancellationToken);

        try
        {
            return (await ReadAllAsync(cancellationToken))
                .Values
                .Where(issue => issue.ClientId == clientId)
                .Where(issue => cleanInstallationId is null
                    || issue.InstallationId.Equals(cleanInstallationId, StringComparison.Ordinal))
                .Where(issue => !appServerInstallationId.HasValue
                    || issue.AppServerInstallationId == appServerInstallationId.Value)
                .Where(issue => cleanQuery is null || MatchesQuery(issue, cleanQuery))
                .OrderByDescending(issue => issue.IssuedAtUtc)
                .ThenByDescending(issue => issue.ActivationIssueId)
                .Take(boundedTake)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Dictionary<Guid, ControlCloudAppActivationIssue>> ReadAllAsync(
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
        var records = await JsonSerializer.DeserializeAsync<IReadOnlyCollection<AppActivationIssueRecord>>(
            stream,
            JsonOptions,
            cancellationToken);

        return (records ?? Array.Empty<AppActivationIssueRecord>())
            .Select(record => ControlCloudAppActivationIssue.Restore(
                record.ActivationIssueId,
                record.ClientId,
                record.InstallationId,
                record.AppServerInstallationId,
                record.ActivationRequestId,
                record.ReplacesActivationIssueId,
                record.FingerprintHash,
                record.ServerPublicKeySha256,
                record.EntitlementVersion,
                record.SigningKeyId,
                record.Status,
                record.RequestedBy,
                record.IssuedAtUtc,
                record.ExpiresAtUtc,
                record.RevokedAtUtc,
                record.RevokedBy,
                record.RevocationReason))
            .GroupBy(issue => issue.ActivationIssueId)
            .ToDictionary(group => group.Key, group => group.Last());
    }

    private async Task WriteAllAsync(
        IReadOnlyCollection<ControlCloudAppActivationIssue> issues,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_storePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var records = issues
            .OrderBy(issue => issue.IssuedAtUtc)
            .ThenBy(issue => issue.ActivationIssueId)
            .Select(issue => new AppActivationIssueRecord(
                issue.ActivationIssueId,
                issue.ClientId,
                issue.InstallationId,
                issue.AppServerInstallationId,
                issue.ActivationRequestId,
                issue.ReplacesActivationIssueId,
                issue.FingerprintHash,
                issue.ServerPublicKeySha256,
                issue.EntitlementVersion,
                issue.SigningKeyId,
                issue.Status,
                issue.RequestedBy,
                issue.IssuedAtUtc,
                issue.ExpiresAtUtc,
                issue.RevokedAtUtc,
                issue.RevokedBy,
                issue.RevocationReason))
            .ToArray();

        await using var stream = new FileStream(
            _storePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read);
        await JsonSerializer.SerializeAsync(stream, records, JsonOptions, cancellationToken);
    }

    private static bool MatchesQuery(
        ControlCloudAppActivationIssue issue,
        string query)
    {
        return Contains(issue.ActivationIssueId.ToString("D"), query)
            || Contains(issue.InstallationId, query)
            || Contains(issue.AppServerInstallationId.ToString("D"), query)
            || Contains(issue.ActivationRequestId.ToString("D"), query)
            || (issue.ReplacesActivationIssueId.HasValue
                && Contains(issue.ReplacesActivationIssueId.Value.ToString("D"), query))
            || Contains(issue.FingerprintHash, query)
            || Contains(issue.ServerPublicKeySha256, query)
            || Contains(issue.SigningKeyId, query)
            || Contains(issue.Status, query)
            || Contains(issue.RequestedBy, query);
    }

    private static bool Contains(string value, string query)
    {
        return value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string ResolveStorePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "App_Data/control-cloud-app-activation-issues.json"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }

    private sealed record AppActivationIssueRecord(
        Guid ActivationIssueId,
        Guid ClientId,
        string InstallationId,
        Guid AppServerInstallationId,
        Guid ActivationRequestId,
        Guid? ReplacesActivationIssueId,
        string FingerprintHash,
        string ServerPublicKeySha256,
        long EntitlementVersion,
        string SigningKeyId,
        string Status,
        string RequestedBy,
        DateTimeOffset IssuedAtUtc,
        DateTimeOffset ExpiresAtUtc,
        DateTimeOffset? RevokedAtUtc,
        string? RevokedBy,
        string? RevocationReason);
}
