namespace SafarSuite.ControlCloud.Domain.Modules.LocalServer;

public sealed record ControlCloudInstallationDeploymentProfile(
    string BootstrapMode,
    string ClientDeploymentMode,
    string SiteId,
    string SiteRole,
    string? ParentSiteId,
    string? BranchCode,
    string? SyncTopologyId)
{
    private const string DefaultBootstrapMode = "OnlineBootstrap";
    private const string DefaultClientDeploymentMode = "OfflineLocal";
    private const string HostedClientDeploymentMode = "HostedSaas";
    private const string StandaloneSiteRole = "Standalone";
    private const string HostedSiteRole = "Hosted";

    public static ControlCloudInstallationDeploymentProfile Create(
        string installationId,
        string? bootstrapMode,
        string? clientDeploymentMode,
        string? siteId,
        string? siteRole,
        string? parentSiteId,
        string? branchCode,
        string? syncTopologyId)
    {
        var normalizedInstallationId = NormalizeRequired(
            installationId,
            nameof(installationId),
            160);
        var normalizedBootstrapMode =
            NormalizeOptional(bootstrapMode, 40) ?? DefaultBootstrapMode;
        var normalizedClientDeploymentMode =
            NormalizeOptional(clientDeploymentMode, 40) ?? DefaultClientDeploymentMode;
        var normalizedSiteId =
            NormalizeOptional(siteId, 160) ?? normalizedInstallationId;
        var normalizedSiteRole = NormalizeOptional(siteRole, 40)
            ?? (normalizedClientDeploymentMode == HostedClientDeploymentMode
                ? HostedSiteRole
                : StandaloneSiteRole);

        return new ControlCloudInstallationDeploymentProfile(
            normalizedBootstrapMode,
            normalizedClientDeploymentMode,
            normalizedSiteId,
            normalizedSiteRole,
            NormalizeOptional(parentSiteId, 160),
            NormalizeOptional(branchCode, 80),
            NormalizeOptional(syncTopologyId, 160));
    }

    private static string NormalizeRequired(
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

    private static string? NormalizeOptional(string? value, int maxLength)
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
