namespace SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

public static class ControlCloudBootstrapModes
{
    public const string OnlineBootstrap = "OnlineBootstrap";
    public const string OfflineAssistedBootstrap = "OfflineAssistedBootstrap";

    public static string NormalizeOrDefault(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? OnlineBootstrap
            : normalized;
    }

    public static bool IsSupported(string? value)
    {
        var normalized = NormalizeOrDefault(value);

        return normalized is OnlineBootstrap or OfflineAssistedBootstrap;
    }
}

public static class SafarSuiteClientDeploymentModes
{
    public const string OfflineLocal = "OfflineLocal";
    public const string BranchToHqSync = "BranchToHqSync";
    public const string CloudSyncMultiBranch = "CloudSyncMultiBranch";
    public const string HostedSaas = "HostedSaas";

    public static string NormalizeOrDefault(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? OfflineLocal
            : normalized;
    }

    public static bool IsSupported(string? value)
    {
        var normalized = NormalizeOrDefault(value);

        return normalized is OfflineLocal
            or BranchToHqSync
            or CloudSyncMultiBranch
            or HostedSaas;
    }
}

public static class SafarSuiteDeploymentDataPlanes
{
    public const string CommercialControl = "CommercialControl";
    public const string OperationalBusinessDataSync = "OperationalBusinessDataSync";
}

public static class SafarSuiteDeploymentSiteRoles
{
    public const string Standalone = "Standalone";
    public const string Hq = "Hq";
    public const string Branch = "Branch";
    public const string Hosted = "Hosted";

    public static string NormalizeOrDefault(
        string? value,
        string clientDeploymentMode)
    {
        var normalized = value?.Trim();

        if (!string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        return clientDeploymentMode == SafarSuiteClientDeploymentModes.HostedSaas
            ? Hosted
            : Standalone;
    }

    public static bool IsSupported(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            || normalized is Standalone or Hq or Branch or Hosted;
    }
}

public sealed record LocalServerDeploymentProfileResponse(
    string BootstrapMode,
    string ClientDeploymentMode,
    string SiteId,
    string SiteRole,
    string? ParentSiteId = null,
    string? BranchCode = null,
    string? SyncTopologyId = null);
