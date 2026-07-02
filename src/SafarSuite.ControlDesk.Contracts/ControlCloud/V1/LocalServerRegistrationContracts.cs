namespace SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

public static class ControlCloudLocalServerBootstrapPackageFormat
{
    public const string Version = "safarsuite-local-server-bootstrap-v1";

    public const string BundleContentType = "application/vnd.safarsuite.local-server-bootstrap+json";
}

public sealed record CreateLocalServerSetupTokenRequest(
    int ExpiresInHours,
    string CreatedBy,
    string DeploymentMode,
    string? ClientDeploymentMode = null,
    string? SiteId = null,
    string? SiteRole = null,
    string? ParentSiteId = null,
    string? BranchCode = null,
    string? SyncTopologyId = null);

public sealed record CreateLocalServerBootstrapPackageRequest(
    int ExpiresInHours,
    string CreatedBy,
    string DeploymentMode,
    string LocalServerVersion,
    string? SafarSuiteAppVersion = null,
    string? ClientDeploymentMode = null,
    string? SiteId = null,
    string? SiteRole = null,
    string? ParentSiteId = null,
    string? BranchCode = null,
    string? SyncTopologyId = null);

public sealed record LocalServerSetupTokenResponse(
    Guid SetupTokenId,
    Guid ClientId,
    string InstallationId,
    string SetupToken,
    string TokenStatus,
    string CreatedBy,
    string DeploymentMode,
    LocalServerDeploymentProfileResponse DeploymentProfile,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc);

public sealed record LocalServerBootstrapPackageEndpointsResponse(
    string RegistrationUrl,
    string EntitlementBundleUrl,
    string HeartbeatUrl,
    string PendingCommandsUrl,
    string? DiagnosticsUrl = null);

public sealed record LocalServerBootstrapPackageSignatureResponse(
    string Algorithm,
    string KeyId,
    string PayloadSha256,
    string Value);

public sealed record LocalServerBootstrapPackageArtifactResponse(
    string ArtifactType,
    string FileName,
    string DownloadUrl,
    string TargetPath,
    string ContentType,
    string Sha256,
    string Content);

public sealed record LocalServerBootstrapRuntimeServiceResponse(
    string ServiceName,
    string ServiceRole,
    bool StartsByDefault,
    string? ComposeProfile,
    string ImageEnvironmentVariable,
    string? PublishedPortEnvironmentVariable,
    string InternalBaseUrl,
    string HealthUrl,
    IReadOnlyCollection<string> DependsOn);

public sealed record LocalServerBootstrapRuntimePlanResponse(
    string RuntimeMode,
    string ComposeProjectName,
    string ConfigDirectory,
    string StateDirectory,
    string LocalServerVersion,
    string SafarSuiteAppVersion,
    IReadOnlyCollection<LocalServerBootstrapRuntimeServiceResponse> Services);

public sealed record LocalServerBootstrapPackagePayloadResponse(
    string FormatVersion,
    Guid BootstrapPackageId,
    Guid SetupTokenId,
    Guid ClientId,
    string InstallationId,
    string DeploymentMode,
    LocalServerDeploymentProfileResponse DeploymentProfile,
    string CloudBaseUrl,
    string LocalServerVersion,
    string SetupToken,
    DateTimeOffset SetupTokenExpiresAtUtc,
    DateTimeOffset GeneratedAtUtc,
    string InstallScriptUrl,
    string InstallCommand,
    IReadOnlyCollection<LocalServerBootstrapPackageArtifactResponse> Artifacts,
    LocalServerBootstrapPackageEndpointsResponse Endpoints,
    LocalServerBootstrapRuntimePlanResponse? RuntimePlan = null);

public sealed record LocalServerSignedBootstrapBundleResponse(
    string PayloadJson,
    LocalServerBootstrapPackagePayloadResponse Payload,
    LocalServerBootstrapPackageSignatureResponse Signature);

public sealed record LocalServerBootstrapPackageResponse(
    string FormatVersion,
    Guid BootstrapPackageId,
    Guid SetupTokenId,
    Guid ClientId,
    string InstallationId,
    string DeploymentMode,
    LocalServerDeploymentProfileResponse DeploymentProfile,
    string CloudBaseUrl,
    string LocalServerVersion,
    string SetupToken,
    DateTimeOffset SetupTokenExpiresAtUtc,
    DateTimeOffset GeneratedAtUtc,
    LocalServerBootstrapPackageEndpointsResponse Endpoints,
    string InstallScriptUrl,
    string InstallCommand,
    IReadOnlyCollection<LocalServerBootstrapPackageArtifactResponse> Artifacts,
    string BundleFileName,
    string BundleContentType,
    string BundleSha256,
    LocalServerSignedBootstrapBundleResponse SignedBundle,
    LocalServerBootstrapRuntimePlanResponse? RuntimePlan = null);

public sealed record RegisterLocalServerInstallationRequest(
    Guid ClientId,
    string SetupToken,
    string LocalServerVersion,
    LocalServerDeploymentProfileResponse? DeploymentProfile = null);

public sealed record LocalServerInstallationRegistrationResponse(
    Guid ClientId,
    string InstallationId,
    string InstallationStatus,
    DateTimeOffset RegisteredAtUtc,
    string LocalServerVersion,
    LocalServerDeploymentProfileResponse? DeploymentProfile = null);
