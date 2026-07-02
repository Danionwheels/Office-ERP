namespace SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

public static class ControlCloudLocalServerDiagnosticsBundleFormat
{
    public const string Version = "safarsuite-local-server-diagnostics-v1";
}

public sealed record LocalServerDiagnosticCheckResponse(
    string Code,
    string Status,
    string Detail);

public sealed record LocalServerDiagnosticModuleResponse(
    string ModuleCode,
    string Status,
    bool IsEnabled);

public sealed record LocalServerDiagnosticEntitlementResponse(
    bool HasCachedEntitlement,
    string? BundleVersion,
    Guid? BundleIssueId,
    long? EntitlementVersion,
    string? Status,
    DateTimeOffset? BundleIssuedAtUtc,
    DateTimeOffset? ImportedAtUtc,
    DateOnly? ValidFrom,
    DateOnly? PaidUntil,
    DateOnly? WarningStartsAt,
    DateOnly? GraceUntil,
    DateOnly? OfflineValidUntil,
    int? AllowedDevices,
    int? AllowedBranches,
    string? SignatureKeyId,
    string? PayloadSha256,
    IReadOnlyCollection<LocalServerDiagnosticModuleResponse> Modules);

public sealed record LocalServerDiagnosticTrustStateResponse(
    long LastAcceptedEntitlementVersion,
    Guid? LastAcceptedBundleIssueId,
    DateTimeOffset? LastAcceptedBundleIssuedAtUtc,
    DateTimeOffset? LastAcceptedAtUtc,
    DateTimeOffset? LastSuccessfulCloudTimeUtc,
    DateTimeOffset? LastLocalCheckAtUtc,
    bool ClockMovedBackwards,
    DateTimeOffset? ClockMovedBackwardsDetectedAtUtc,
    string? LastClockWarning,
    string? LastReplayWarning,
    DateTimeOffset? LastReplayWarningAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record LocalServerDiagnosticRuntimeResponse(
    string Version,
    string BuildChannel,
    string BuildCommit,
    string RuntimeMode,
    string MachineName,
    string OperatingSystem,
    string HostArchitecture,
    int ProcessorCount,
    bool? DockerAvailable,
    string? DockerVersion,
    bool? DockerComposeAvailable,
    string? DockerComposeVersion);

public sealed record LocalServerDiagnosticBootstrapResponse(
    string ConfigDirectory,
    string BootstrapStatus,
    string? BootstrapConfigSha256,
    string? ComposeFileSha256,
    string? EnvironmentFileSha256,
    DateTimeOffset? LastRegistrationAttemptUtc,
    DateTimeOffset? LastRegistrationSucceededAtUtc,
    DateTimeOffset? LastHeartbeatSentAtUtc,
    DateTimeOffset? LastEntitlementPullAtUtc);

public sealed record LocalServerDiagnosticServiceResponse(
    string ServiceName,
    string ExpectedState,
    string? CurrentState,
    string? ContainerName,
    DateTimeOffset? LastStartedAtUtc,
    string? Detail);

public sealed record LocalServerDiagnosticRecentErrorResponse(
    string Source,
    string Severity,
    string Message,
    DateTimeOffset? OccurredAtUtc);

public sealed record LocalServerDiagnosticImportAuditResponse(
    Guid AuditRecordId,
    string InstallationId,
    Guid? ClientId,
    string ImportSource,
    string ResultStatus,
    long? EntitlementVersion,
    Guid? BundleIssueId,
    string? FailureCode,
    string? Detail,
    string? PayloadSha256,
    string? SignatureKeyId,
    DateTimeOffset OccurredAtUtc);

public sealed record LocalServerDiagnosticBundleResponse(
    string FormatVersion,
    Guid DiagnosticBundleId,
    Guid ClientId,
    string InstallationId,
    DateTimeOffset GeneratedAtUtc,
    string GeneratedBy,
    string Reason,
    string LocalServerVersion,
    string MachineName,
    string OperatingSystem,
    string LicenseStatus,
    LocalServerDiagnosticEntitlementResponse CachedEntitlement,
    LocalServerDiagnosticTrustStateResponse? TrustState,
    IReadOnlyCollection<LocalServerDiagnosticCheckResponse> Checks,
    LocalServerDiagnosticRuntimeResponse? Runtime = null,
    LocalServerDiagnosticBootstrapResponse? Bootstrap = null,
    IReadOnlyCollection<LocalServerDiagnosticServiceResponse>? Services = null,
    IReadOnlyCollection<LocalServerDiagnosticRecentErrorResponse>? RecentErrors = null,
    IReadOnlyCollection<LocalServerDiagnosticImportAuditResponse>? ImportAudit = null,
    LocalServerDeploymentProfileResponse? DeploymentProfile = null);

public sealed record UploadLocalServerDiagnosticsRequest(
    Guid ClientId,
    string UploadedBy,
    string Reason,
    LocalServerDiagnosticBundleResponse Bundle);

public sealed record LocalServerDiagnosticsUploadResponse(
    Guid DiagnosticReportId,
    Guid ClientId,
    string InstallationId,
    string Status,
    DateTimeOffset ReceivedAtUtc);

public sealed record LocalServerDiagnosticReportResponse(
    Guid DiagnosticReportId,
    Guid ClientId,
    string InstallationId,
    string Status,
    DateTimeOffset ReceivedAtUtc,
    DateTimeOffset GeneratedAtUtc,
    string UploadedBy,
    string Reason,
    string LocalServerVersion,
    string LicenseStatus,
    LocalServerDiagnosticBundleResponse Bundle);
