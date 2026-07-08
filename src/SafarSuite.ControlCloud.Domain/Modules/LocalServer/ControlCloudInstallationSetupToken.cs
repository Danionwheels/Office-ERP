namespace SafarSuite.ControlCloud.Domain.Modules.LocalServer;

public sealed class ControlCloudInstallationSetupToken
{
    private ControlCloudInstallationSetupToken(
        Guid setupTokenId,
        Guid clientId,
        string installationId,
        string tokenHash,
        string status,
        string createdBy,
        string deploymentMode,
        string? clientDeploymentMode,
        string? siteId,
        string? siteRole,
        string? parentSiteId,
        string? branchCode,
        string? syncTopologyId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? consumedAtUtc,
        string? consumedLocalServerVersion,
        Guid? bootstrapPackageId,
        DateTimeOffset? bootstrapPackageGeneratedAtUtc,
        string? packageLocalServerVersion,
        string? packageSafarSuiteAppVersion,
        string? packageBundleFileName,
        string? packageBundleSha256)
    {
        SetupTokenId = setupTokenId;
        ClientId = clientId;
        InstallationId = NormalizeRequiredText(installationId, nameof(installationId), 160);
        TokenHash = NormalizeRequiredText(tokenHash, nameof(tokenHash), 128);
        Status = NormalizeRequiredText(status, nameof(status), 32);
        CreatedBy = NormalizeRequiredText(createdBy, nameof(createdBy), 120);
        DeploymentProfile = ControlCloudInstallationDeploymentProfile.Create(
            InstallationId,
            deploymentMode,
            clientDeploymentMode,
            siteId,
            siteRole,
            parentSiteId,
            branchCode,
            syncTopologyId);
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        ConsumedAtUtc = consumedAtUtc;
        ConsumedLocalServerVersion = NormalizeOptionalText(consumedLocalServerVersion, 80);
        BootstrapPackageId = bootstrapPackageId == Guid.Empty ? null : bootstrapPackageId;
        BootstrapPackageGeneratedAtUtc = bootstrapPackageGeneratedAtUtc;
        PackageLocalServerVersion = NormalizeOptionalText(packageLocalServerVersion, 80);
        PackageSafarSuiteAppVersion = NormalizeOptionalText(packageSafarSuiteAppVersion, 80);
        PackageBundleFileName = NormalizeOptionalText(packageBundleFileName, 260);
        PackageBundleSha256 = NormalizeOptionalText(packageBundleSha256, 128);
    }

    public Guid SetupTokenId { get; }

    public Guid ClientId { get; }

    public string InstallationId { get; }

    public string TokenHash { get; }

    public string Status { get; private set; }

    public string CreatedBy { get; }

    public string DeploymentMode => DeploymentProfile.BootstrapMode;

    public ControlCloudInstallationDeploymentProfile DeploymentProfile { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset ExpiresAtUtc { get; }

    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    public string? ConsumedLocalServerVersion { get; private set; }

    public Guid? BootstrapPackageId { get; private set; }

    public DateTimeOffset? BootstrapPackageGeneratedAtUtc { get; private set; }

    public string? PackageLocalServerVersion { get; private set; }

    public string? PackageSafarSuiteAppVersion { get; private set; }

    public string? PackageBundleFileName { get; private set; }

    public string? PackageBundleSha256 { get; private set; }

    public bool HasBootstrapPackage => BootstrapPackageId.HasValue;

    public static ControlCloudInstallationSetupToken Create(
        Guid setupTokenId,
        Guid clientId,
        string installationId,
        string tokenHash,
        string createdBy,
        string deploymentMode,
        string? clientDeploymentMode,
        string? siteId,
        string? siteRole,
        string? parentSiteId,
        string? branchCode,
        string? syncTopologyId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (setupTokenId == Guid.Empty)
        {
            throw new ArgumentException("Setup token id is required.", nameof(setupTokenId));
        }

        if (clientId == Guid.Empty)
        {
            throw new ArgumentException("Client id is required.", nameof(clientId));
        }

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException(
                "Setup token expiry must be after created time.",
                nameof(expiresAtUtc));
        }

        return new ControlCloudInstallationSetupToken(
            setupTokenId,
            clientId,
            installationId,
            tokenHash,
            ControlCloudInstallationSetupTokenStatuses.Pending,
            createdBy,
            deploymentMode,
            clientDeploymentMode,
            siteId,
            siteRole,
            parentSiteId,
            branchCode,
            syncTopologyId,
            createdAtUtc,
            expiresAtUtc,
            consumedAtUtc: null,
            consumedLocalServerVersion: null,
            bootstrapPackageId: null,
            bootstrapPackageGeneratedAtUtc: null,
            packageLocalServerVersion: null,
            packageSafarSuiteAppVersion: null,
            packageBundleFileName: null,
            packageBundleSha256: null);
    }

    public static ControlCloudInstallationSetupToken Restore(
        Guid setupTokenId,
        Guid clientId,
        string installationId,
        string tokenHash,
        string status,
        string createdBy,
        string deploymentMode,
        string? clientDeploymentMode,
        string? siteId,
        string? siteRole,
        string? parentSiteId,
        string? branchCode,
        string? syncTopologyId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? consumedAtUtc,
        string? consumedLocalServerVersion,
        Guid? bootstrapPackageId = null,
        DateTimeOffset? bootstrapPackageGeneratedAtUtc = null,
        string? packageLocalServerVersion = null,
        string? packageSafarSuiteAppVersion = null,
        string? packageBundleFileName = null,
        string? packageBundleSha256 = null)
    {
        return new ControlCloudInstallationSetupToken(
            setupTokenId,
            clientId,
            installationId,
            tokenHash,
            status,
            createdBy,
            deploymentMode,
            clientDeploymentMode,
            siteId,
            siteRole,
            parentSiteId,
            branchCode,
            syncTopologyId,
            createdAtUtc,
            expiresAtUtc,
            consumedAtUtc,
            consumedLocalServerVersion,
            bootstrapPackageId,
            bootstrapPackageGeneratedAtUtc,
            packageLocalServerVersion,
            packageSafarSuiteAppVersion,
            packageBundleFileName,
            packageBundleSha256);
    }

    public bool IsPendingAt(DateTimeOffset now)
    {
        return Status == ControlCloudInstallationSetupTokenStatuses.Pending
            && ExpiresAtUtc > now;
    }

    public void Consume(
        string localServerVersion,
        DateTimeOffset consumedAtUtc)
    {
        if (!IsPendingAt(consumedAtUtc))
        {
            throw new InvalidOperationException(
                "Setup token is not pending or has expired.");
        }

        Status = ControlCloudInstallationSetupTokenStatuses.Consumed;
        ConsumedAtUtc = consumedAtUtc;
        ConsumedLocalServerVersion = NormalizeOptionalText(localServerVersion, 80);
    }

    public void AttachBootstrapPackage(
        Guid bootstrapPackageId,
        DateTimeOffset generatedAtUtc,
        string localServerVersion,
        string safarSuiteAppVersion,
        string bundleFileName,
        string bundleSha256)
    {
        if (bootstrapPackageId == Guid.Empty)
        {
            throw new ArgumentException("Bootstrap package id is required.", nameof(bootstrapPackageId));
        }

        BootstrapPackageId = bootstrapPackageId;
        BootstrapPackageGeneratedAtUtc = generatedAtUtc;
        PackageLocalServerVersion = NormalizeRequiredText(localServerVersion, nameof(localServerVersion), 80);
        PackageSafarSuiteAppVersion = NormalizeRequiredText(safarSuiteAppVersion, nameof(safarSuiteAppVersion), 80);
        PackageBundleFileName = NormalizeRequiredText(bundleFileName, nameof(bundleFileName), 260);
        PackageBundleSha256 = NormalizeRequiredText(bundleSha256, nameof(bundleSha256), 128);
    }

    private static string NormalizeRequiredText(
        string value,
        string parameterName,
        int maxLength)
    {
        var normalized = value.Trim();

        if (normalized.Length == 0)
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }

    private static string? NormalizeOptionalText(string? value, int maxLength)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }
}

public static class ControlCloudInstallationSetupTokenStatuses
{
    public const string Pending = "Pending";
    public const string Consumed = "Consumed";
    public const string Revoked = "Revoked";
}
