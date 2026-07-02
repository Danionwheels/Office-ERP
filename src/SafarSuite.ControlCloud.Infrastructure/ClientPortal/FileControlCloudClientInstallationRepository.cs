using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class FileControlCloudClientInstallationRepository : IControlCloudClientInstallationRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileControlCloudClientInstallationRepository(ControlCloudEntitlementSigningOptions options)
    {
        _storePath = ResolveStorePath(options.InstallationStorePath);
    }

    public async Task<ControlCloudClientInstallation?> GetByInstallationIdAsync(
        string installationId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var installations = await ReadAllAsync(cancellationToken);

            return installations.TryGetValue(installationId.Trim(), out var installation)
                ? installation
                : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AddAsync(
        ControlCloudClientInstallation installation,
        CancellationToken cancellationToken = default)
    {
        await SaveAsync(installation, cancellationToken);
    }

    public async Task SaveAsync(
        ControlCloudClientInstallation installation,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var installations = await ReadAllAsync(cancellationToken);
            installations[installation.InstallationId] = installation;

            await WriteAllAsync(installations, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Dictionary<string, ControlCloudClientInstallation>> ReadAllAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_storePath))
        {
            return new Dictionary<string, ControlCloudClientInstallation>(StringComparer.Ordinal);
        }

        await using var stream = new FileStream(
            _storePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);
        var records = await JsonSerializer.DeserializeAsync<IReadOnlyCollection<InstallationRecord>>(
            stream,
            JsonOptions,
            cancellationToken);

        return (records ?? Array.Empty<InstallationRecord>())
            .Select(record => ControlCloudClientInstallation.Restore(
                record.ClientId,
                record.InstallationId,
                record.Status,
                record.RegisteredAtUtc,
                record.LastBundleIssuedAtUtc,
                record.LatestEntitlementVersion,
                record.BootstrapMode,
                record.ClientDeploymentMode,
                record.SiteId,
                record.SiteRole,
                record.ParentSiteId,
                record.BranchCode,
                record.SyncTopologyId))
            .GroupBy(installation => installation.InstallationId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
    }

    private async Task WriteAllAsync(
        IReadOnlyDictionary<string, ControlCloudClientInstallation> installations,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_storePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var records = installations.Values
            .OrderBy(installation => installation.InstallationId, StringComparer.Ordinal)
            .Select(installation => new InstallationRecord(
                installation.ClientId,
                installation.InstallationId,
                installation.Status,
                installation.RegisteredAtUtc,
                installation.LastBundleIssuedAtUtc,
                installation.LatestEntitlementVersion,
                installation.DeploymentProfile.BootstrapMode,
                installation.DeploymentProfile.ClientDeploymentMode,
                installation.DeploymentProfile.SiteId,
                installation.DeploymentProfile.SiteRole,
                installation.DeploymentProfile.ParentSiteId,
                installation.DeploymentProfile.BranchCode,
                installation.DeploymentProfile.SyncTopologyId))
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
            ? "App_Data/control-cloud-client-installations.json"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }

    private sealed record InstallationRecord(
        Guid ClientId,
        string InstallationId,
        string Status,
        DateTimeOffset RegisteredAtUtc,
        DateTimeOffset? LastBundleIssuedAtUtc,
        long LatestEntitlementVersion,
        string? BootstrapMode,
        string? ClientDeploymentMode,
        string? SiteId,
        string? SiteRole,
        string? ParentSiteId,
        string? BranchCode,
        string? SyncTopologyId);
}
