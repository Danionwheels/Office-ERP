namespace SafarSuite.LocalServer.Domain.Registration;

public sealed record LocalServerBootstrapConfiguration(
    string FormatVersion,
    Guid BootstrapPackageId,
    Guid SetupTokenId,
    Guid ClientId,
    string InstallationId,
    string DeploymentMode,
    LocalServerBootstrapDeploymentProfile DeploymentProfile,
    string CloudBaseUrl,
    string LocalServerVersion,
    string SetupToken,
    DateTimeOffset SetupTokenExpiresAtUtc,
    DateTimeOffset GeneratedAtUtc,
    LocalServerBootstrapEndpoints Endpoints,
    LocalServerBootstrapRuntimePlan? RuntimePlan,
    string PayloadJson,
    string SignatureAlgorithm,
    string SignatureKeyId,
    string PayloadSha256,
    string SignatureValue,
    DateTimeOffset ImportedAtUtc,
    string RegistrationStatus,
    DateTimeOffset? LastRegistrationAttemptUtc = null,
    DateTimeOffset? LastRegistrationSucceededAtUtc = null,
    string? LastRegistrationFailureCode = null,
    string? LastRegistrationFailureDetail = null)
{
    public LocalServerBootstrapConfiguration RecordRegistrationAttempt(
        DateTimeOffset attemptedAtUtc)
    {
        return this with
        {
            RegistrationStatus = LocalServerBootstrapRegistrationStatuses.RegistrationPending,
            LastRegistrationAttemptUtc = attemptedAtUtc,
            LastRegistrationFailureCode = null,
            LastRegistrationFailureDetail = null
        };
    }

    public LocalServerBootstrapConfiguration RecordRegistrationSucceeded(
        DateTimeOffset succeededAtUtc)
    {
        return this with
        {
            RegistrationStatus = LocalServerBootstrapRegistrationStatuses.Registered,
            LastRegistrationSucceededAtUtc = succeededAtUtc,
            LastRegistrationFailureCode = null,
            LastRegistrationFailureDetail = null
        };
    }

    public LocalServerBootstrapConfiguration RecordRegistrationFailed(
        string failureCode,
        string detail)
    {
        return this with
        {
            RegistrationStatus = LocalServerBootstrapRegistrationStatuses.RegistrationFailed,
            LastRegistrationFailureCode = NormalizeOptionalText(failureCode, 120),
            LastRegistrationFailureDetail = NormalizeOptionalText(detail, 500)
        };
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

public sealed record LocalServerBootstrapDeploymentProfile(
    string BootstrapMode,
    string ClientDeploymentMode,
    string SiteId,
    string SiteRole,
    string? ParentSiteId,
    string? BranchCode,
    string? SyncTopologyId);

public sealed record LocalServerBootstrapEndpoints(
    string RegistrationUrl,
    string EntitlementBundleUrl,
    string HeartbeatUrl,
    string PendingCommandsUrl,
    string? DiagnosticsUrl);

public sealed record LocalServerBootstrapRuntimePlan(
    string RuntimeMode,
    string ComposeProjectName,
    string ConfigDirectory,
    string StateDirectory,
    string LocalServerVersion,
    string SafarSuiteAppVersion,
    IReadOnlyCollection<LocalServerBootstrapRuntimeService> Services);

public sealed record LocalServerBootstrapRuntimeService(
    string ServiceName,
    string ServiceRole,
    bool StartsByDefault,
    string? ComposeProfile,
    string ImageEnvironmentVariable,
    string? PublishedPortEnvironmentVariable,
    string InternalBaseUrl,
    string HealthUrl,
    IReadOnlyCollection<string> DependsOn);

public static class LocalServerBootstrapRegistrationStatuses
{
    public const string Imported = "Imported";
    public const string RegistrationPending = "RegistrationPending";
    public const string Registered = "Registered";
    public const string RegistrationFailed = "RegistrationFailed";
}
