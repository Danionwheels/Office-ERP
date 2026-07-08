using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Infrastructure.LocalServer;

public sealed class FileControlCloudInstallationSetupTokenRepository
    : IControlCloudInstallationSetupTokenRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileControlCloudInstallationSetupTokenRepository(
        ControlCloudSetupTokenOptions options)
    {
        _storePath = ResolveStorePath(options.TokenStorePath);
    }

    public async Task<ControlCloudInstallationSetupToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var setupTokens = await ReadAllAsync(cancellationToken);

            return setupTokens.Values.FirstOrDefault(
                setupToken => string.Equals(
                    setupToken.TokenHash,
                    tokenHash.Trim(),
                    StringComparison.Ordinal));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AddAsync(
        ControlCloudInstallationSetupToken setupToken,
        CancellationToken cancellationToken = default)
    {
        await SaveAsync(setupToken, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ControlCloudInstallationSetupToken>> ListBootstrapPackagesAsync(
        Guid clientId,
        string installationId,
        int take,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var normalizedInstallationId = installationId.Trim();
            var setupTokens = await ReadAllAsync(cancellationToken);

            return setupTokens.Values
                .Where(setupToken => setupToken.ClientId == clientId
                    && string.Equals(setupToken.InstallationId, normalizedInstallationId, StringComparison.Ordinal)
                    && setupToken.HasBootstrapPackage)
                .OrderByDescending(setupToken => setupToken.BootstrapPackageGeneratedAtUtc ?? setupToken.CreatedAtUtc)
                .ThenByDescending(setupToken => setupToken.CreatedAtUtc)
                .Take(take)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(
        ControlCloudInstallationSetupToken setupToken,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var setupTokens = await ReadAllAsync(cancellationToken);
            setupTokens[setupToken.SetupTokenId] = setupToken;

            await WriteAllAsync(setupTokens, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Dictionary<Guid, ControlCloudInstallationSetupToken>> ReadAllAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_storePath))
        {
            return new Dictionary<Guid, ControlCloudInstallationSetupToken>();
        }

        await using var stream = new FileStream(
            _storePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);
        var records = await JsonSerializer.DeserializeAsync<IReadOnlyCollection<SetupTokenRecord>>(
            stream,
            JsonOptions,
            cancellationToken);

        return (records ?? Array.Empty<SetupTokenRecord>())
            .Select(record => ControlCloudInstallationSetupToken.Restore(
                record.SetupTokenId,
                record.ClientId,
                record.InstallationId,
                record.TokenHash,
                record.Status,
                record.CreatedBy,
                record.DeploymentMode,
                record.ClientDeploymentMode,
                record.SiteId,
                record.SiteRole,
                record.ParentSiteId,
                record.BranchCode,
                record.SyncTopologyId,
                record.CreatedAtUtc,
                record.ExpiresAtUtc,
                record.ConsumedAtUtc,
                record.ConsumedLocalServerVersion,
                record.BootstrapPackageId,
                record.BootstrapPackageGeneratedAtUtc,
                record.PackageLocalServerVersion,
                record.PackageSafarSuiteAppVersion,
                record.PackageBundleFileName,
                record.PackageBundleSha256))
            .GroupBy(setupToken => setupToken.SetupTokenId)
            .ToDictionary(group => group.Key, group => group.Last());
    }

    private async Task WriteAllAsync(
        IReadOnlyDictionary<Guid, ControlCloudInstallationSetupToken> setupTokens,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_storePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var records = setupTokens.Values
            .OrderBy(setupToken => setupToken.CreatedAtUtc)
            .Select(setupToken => new SetupTokenRecord(
                setupToken.SetupTokenId,
                setupToken.ClientId,
                setupToken.InstallationId,
                setupToken.TokenHash,
                setupToken.Status,
                setupToken.CreatedBy,
                setupToken.DeploymentMode,
                setupToken.DeploymentProfile.ClientDeploymentMode,
                setupToken.DeploymentProfile.SiteId,
                setupToken.DeploymentProfile.SiteRole,
                setupToken.DeploymentProfile.ParentSiteId,
                setupToken.DeploymentProfile.BranchCode,
                setupToken.DeploymentProfile.SyncTopologyId,
                setupToken.CreatedAtUtc,
                setupToken.ExpiresAtUtc,
                setupToken.ConsumedAtUtc,
                setupToken.ConsumedLocalServerVersion,
                setupToken.BootstrapPackageId,
                setupToken.BootstrapPackageGeneratedAtUtc,
                setupToken.PackageLocalServerVersion,
                setupToken.PackageSafarSuiteAppVersion,
                setupToken.PackageBundleFileName,
                setupToken.PackageBundleSha256))
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
            ? "App_Data/control-cloud-installation-setup-tokens.json"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }

    private sealed record SetupTokenRecord(
        Guid SetupTokenId,
        Guid ClientId,
        string InstallationId,
        string TokenHash,
        string Status,
        string CreatedBy,
        string DeploymentMode,
        string? ClientDeploymentMode,
        string? SiteId,
        string? SiteRole,
        string? ParentSiteId,
        string? BranchCode,
        string? SyncTopologyId,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset ExpiresAtUtc,
        DateTimeOffset? ConsumedAtUtc,
        string? ConsumedLocalServerVersion,
        Guid? BootstrapPackageId = null,
        DateTimeOffset? BootstrapPackageGeneratedAtUtc = null,
        string? PackageLocalServerVersion = null,
        string? PackageSafarSuiteAppVersion = null,
        string? PackageBundleFileName = null,
        string? PackageBundleSha256 = null);
}
