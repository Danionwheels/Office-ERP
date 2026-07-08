using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Infrastructure.LocalServer;

public sealed class FileControlCloudFirstManagerSetupTokenIssueRepository
    : IControlCloudFirstManagerSetupTokenIssueRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileControlCloudFirstManagerSetupTokenIssueRepository(
        ControlCloudFirstManagerSetupTokenOptions options)
    {
        _storePath = ResolveStorePath(options.IssueStorePath);
    }

    public async Task AddAsync(
        ControlCloudFirstManagerSetupTokenIssue issue,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var issues = await ReadAllAsync(cancellationToken);
            issues[issue.TokenId] = issue;

            await WriteAllAsync(issues.Values, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ControlCloudFirstManagerSetupTokenIssue?> GetByTokenIdAsync(
        Guid tokenId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var issues = await ReadAllAsync(cancellationToken);

            return issues.TryGetValue(tokenId, out var issue)
                ? issue
                : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Dictionary<Guid, ControlCloudFirstManagerSetupTokenIssue>> ReadAllAsync(
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
        var records = await JsonSerializer.DeserializeAsync<IReadOnlyCollection<FirstManagerSetupTokenIssueRecord>>(
            stream,
            JsonOptions,
            cancellationToken);

        return (records ?? Array.Empty<FirstManagerSetupTokenIssueRecord>())
            .Select(record => ControlCloudFirstManagerSetupTokenIssue.Restore(
                record.TokenId,
                record.ClientId,
                record.InstallationId,
                record.PendingDeviceRequestId,
                record.ManagerDisplayName,
                record.ManagerEmail,
                record.CreatedBy,
                record.SigningKeyId,
                record.PayloadSha256,
                record.IssuedAtUtc,
                record.ExpiresAtUtc))
            .GroupBy(issue => issue.TokenId)
            .ToDictionary(group => group.Key, group => group.Last());
    }

    private async Task WriteAllAsync(
        IReadOnlyCollection<ControlCloudFirstManagerSetupTokenIssue> issues,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_storePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var records = issues
            .OrderBy(issue => issue.IssuedAtUtc)
            .ThenBy(issue => issue.TokenId)
            .Select(issue => new FirstManagerSetupTokenIssueRecord(
                issue.TokenId,
                issue.ClientId,
                issue.InstallationId,
                issue.PendingDeviceRequestId,
                issue.ManagerDisplayName,
                issue.ManagerEmail,
                issue.CreatedBy,
                issue.SigningKeyId,
                issue.PayloadSha256,
                issue.IssuedAtUtc,
                issue.ExpiresAtUtc))
            .ToArray();

        await using var stream = new FileStream(
            _storePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read);
        await JsonSerializer.SerializeAsync(stream, records, JsonOptions, cancellationToken);
    }

    private static string ResolveStorePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "App_Data/control-cloud-first-manager-setup-token-issues.json"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }

    private sealed record FirstManagerSetupTokenIssueRecord(
        Guid TokenId,
        Guid ClientId,
        string InstallationId,
        Guid PendingDeviceRequestId,
        string ManagerDisplayName,
        string? ManagerEmail,
        string CreatedBy,
        string SigningKeyId,
        string PayloadSha256,
        DateTimeOffset IssuedAtUtc,
        DateTimeOffset ExpiresAtUtc);
}
