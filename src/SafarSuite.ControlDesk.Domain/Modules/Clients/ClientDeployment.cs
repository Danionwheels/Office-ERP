using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Clients;

public sealed class ClientDeployment : Entity<ClientDeploymentId>
{
    private ClientDeployment()
    {
        DisplayName = string.Empty;
        InstallationId = string.Empty;
        BootstrapMode = string.Empty;
        ClientDeploymentMode = string.Empty;
        SiteId = string.Empty;
        SiteRole = string.Empty;
        LocalServerVersion = string.Empty;
        SafarSuiteAppVersion = string.Empty;
    }

    private ClientDeployment(
        ClientDeploymentId id,
        ClientId clientId,
        string displayName,
        string installationId,
        string bootstrapMode,
        string clientDeploymentMode,
        string siteId,
        string siteRole,
        string? parentSiteId,
        string? branchCode,
        string? syncTopologyId,
        string localServerVersion,
        string? safarSuiteAppVersion,
        bool isPrimary,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        ClientId = clientId;
        DisplayName = CleanRequired(displayName, nameof(displayName), 128);
        InstallationId = CleanRequired(installationId, nameof(installationId), 160);
        BootstrapMode = CleanRequired(bootstrapMode, nameof(bootstrapMode), 64);
        ClientDeploymentMode = CleanRequired(clientDeploymentMode, nameof(clientDeploymentMode), 64);
        SiteId = CleanRequired(siteId, nameof(siteId), 96);
        SiteRole = CleanRequired(siteRole, nameof(siteRole), 64);
        ParentSiteId = CleanOptional(parentSiteId, 96);
        BranchCode = CleanOptional(branchCode, 64);
        SyncTopologyId = CleanOptional(syncTopologyId, 96);
        LocalServerVersion = CleanRequired(localServerVersion, nameof(localServerVersion), 64);
        SafarSuiteAppVersion = CleanOptional(safarSuiteAppVersion, 64) ?? LocalServerVersion;
        IsPrimary = isPrimary;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public ClientId ClientId { get; private set; }

    public string DisplayName { get; private set; }

    public string InstallationId { get; private set; }

    public string BootstrapMode { get; private set; }

    public string ClientDeploymentMode { get; private set; }

    public string SiteId { get; private set; }

    public string SiteRole { get; private set; }

    public string? ParentSiteId { get; private set; }

    public string? BranchCode { get; private set; }

    public string? SyncTopologyId { get; private set; }

    public string LocalServerVersion { get; private set; }

    public string SafarSuiteAppVersion { get; private set; }

    public bool IsPrimary { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static ClientDeployment Create(
        ClientDeploymentId id,
        ClientId clientId,
        string displayName,
        string installationId,
        string bootstrapMode,
        string clientDeploymentMode,
        string siteId,
        string siteRole,
        string? parentSiteId,
        string? branchCode,
        string? syncTopologyId,
        string localServerVersion,
        string? safarSuiteAppVersion,
        bool isPrimary,
        DateTimeOffset createdAtUtc)
    {
        return new ClientDeployment(
            id,
            clientId,
            displayName,
            installationId,
            bootstrapMode,
            clientDeploymentMode,
            siteId,
            siteRole,
            parentSiteId,
            branchCode,
            syncTopologyId,
            localServerVersion,
            safarSuiteAppVersion,
            isPrimary,
            createdAtUtc);
    }

    public void UpdateProfile(
        string displayName,
        string bootstrapMode,
        string clientDeploymentMode,
        string siteId,
        string siteRole,
        string? parentSiteId,
        string? branchCode,
        string? syncTopologyId,
        string localServerVersion,
        string? safarSuiteAppVersion,
        DateTimeOffset updatedAtUtc)
    {
        DisplayName = CleanRequired(displayName, nameof(displayName), 128);
        BootstrapMode = CleanRequired(bootstrapMode, nameof(bootstrapMode), 64);
        ClientDeploymentMode = CleanRequired(clientDeploymentMode, nameof(clientDeploymentMode), 64);
        SiteId = CleanRequired(siteId, nameof(siteId), 96);
        SiteRole = CleanRequired(siteRole, nameof(siteRole), 64);
        ParentSiteId = CleanOptional(parentSiteId, 96);
        BranchCode = CleanOptional(branchCode, 64);
        SyncTopologyId = CleanOptional(syncTopologyId, 96);
        LocalServerVersion = CleanRequired(localServerVersion, nameof(localServerVersion), 64);
        SafarSuiteAppVersion = CleanOptional(safarSuiteAppVersion, 64) ?? LocalServerVersion;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void SetPrimary(bool isPrimary, DateTimeOffset updatedAtUtc)
    {
        IsPrimary = isPrimary;
        UpdatedAtUtc = updatedAtUtc;
    }

    private static string CleanRequired(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        var cleanValue = value.Trim();

        if (cleanValue.Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maxLength} characters.",
                parameterName);
        }

        return cleanValue;
    }

    private static string? CleanOptional(string? value, int maxLength)
    {
        var cleanValue = value?.Trim();

        if (string.IsNullOrWhiteSpace(cleanValue))
        {
            return null;
        }

        if (cleanValue.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", nameof(value));
        }

        return cleanValue;
    }
}
