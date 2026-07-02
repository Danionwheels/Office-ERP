namespace SafarSuite.ControlCloud.Domain.Modules.LocalServer;

public sealed record ControlCloudSignedBootstrapPackage(
    string PayloadJson,
    ControlCloudBootstrapPackagePayload Payload,
    ControlCloudBootstrapPackageSignature Signature);

public sealed record ControlCloudBootstrapPackagePayload(
    string FormatVersion,
    Guid BootstrapPackageId,
    Guid SetupTokenId,
    Guid ClientId,
    string InstallationId,
    string DeploymentMode,
    ControlCloudInstallationDeploymentProfile DeploymentProfile,
    string CloudBaseUrl,
    string LocalServerVersion,
    string SetupToken,
    DateTimeOffset SetupTokenExpiresAtUtc,
    DateTimeOffset GeneratedAtUtc,
    string InstallScriptUrl,
    string InstallCommand,
    IReadOnlyCollection<ControlCloudBootstrapPackageArtifact> Artifacts,
    ControlCloudBootstrapRuntimePlan RuntimePlan,
    ControlCloudBootstrapPackageEndpoints Endpoints);

public sealed record ControlCloudBootstrapPackageArtifact(
    string ArtifactType,
    string FileName,
    string DownloadUrl,
    string TargetPath,
    string ContentType,
    string Sha256,
    string Content);

public sealed record ControlCloudBootstrapPackageEndpoints(
    string RegistrationUrl,
    string EntitlementBundleUrl,
    string HeartbeatUrl,
    string PendingCommandsUrl,
    string DiagnosticsUrl);

public sealed record ControlCloudBootstrapRuntimeService(
    string ServiceName,
    string ServiceRole,
    bool StartsByDefault,
    string? ComposeProfile,
    string ImageEnvironmentVariable,
    string? PublishedPortEnvironmentVariable,
    string InternalBaseUrl,
    string HealthUrl,
    IReadOnlyCollection<string> DependsOn);

public sealed record ControlCloudBootstrapRuntimePlan(
    string RuntimeMode,
    string ComposeProjectName,
    string ConfigDirectory,
    string StateDirectory,
    string LocalServerVersion,
    string SafarSuiteAppVersion,
    IReadOnlyCollection<ControlCloudBootstrapRuntimeService> Services);

public sealed record ControlCloudBootstrapPackageSignature(
    string Algorithm,
    string KeyId,
    string PayloadSha256,
    string Value);
