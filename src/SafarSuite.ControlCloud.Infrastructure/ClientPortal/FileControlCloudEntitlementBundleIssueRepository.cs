using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class FileControlCloudEntitlementBundleIssueRepository
    : IControlCloudEntitlementBundleIssueRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileControlCloudEntitlementBundleIssueRepository(ControlCloudEntitlementSigningOptions options)
    {
        _storePath = ResolveStorePath(options.BundleIssueStorePath);
    }

    public async Task AddAsync(
        ControlCloudEntitlementBundleIssue issue,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var issues = await ReadAllAsync(cancellationToken);
            issues.Add(issue);

            await WriteAllAsync(issues, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ControlCloudEntitlementBundleIssue?> GetLatestByInstallationIdAsync(
        string installationId,
        CancellationToken cancellationToken = default)
    {
        var cleanInstallationId = installationId.Trim();

        await _gate.WaitAsync(cancellationToken);

        try
        {
            return (await ReadAllAsync(cancellationToken))
                .Where(issue => issue.InstallationId == cleanInstallationId)
                .OrderBy(issue => issue.IssuedAtUtc)
                .ThenBy(issue => issue.BundleIssueId)
                .LastOrDefault();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<ControlCloudEntitlementBundleIssue>> ReadAllAsync(
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
        var issues = await JsonSerializer.DeserializeAsync<List<ControlCloudEntitlementBundleIssue>>(
            stream,
            JsonOptions,
            cancellationToken);

        return issues ?? [];
    }

    private async Task WriteAllAsync(
        IReadOnlyCollection<ControlCloudEntitlementBundleIssue> issues,
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
            issues.OrderBy(issue => issue.IssuedAtUtc).ThenBy(issue => issue.BundleIssueId).ToArray(),
            JsonOptions,
            cancellationToken);
    }

    private static string ResolveStorePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "App_Data/control-cloud-entitlement-bundle-issues.json"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}
