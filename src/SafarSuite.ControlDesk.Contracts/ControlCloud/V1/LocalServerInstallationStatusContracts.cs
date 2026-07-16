namespace SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

public sealed record ControlCloudInstallationStatusResponse(
    Guid ClientId,
    string InstallationId,
    string InstallationStatus,
    LocalServerDeploymentProfileResponse DeploymentProfile,
    DateTimeOffset RegisteredAtUtc,
    DateTimeOffset? LastBundleIssuedAtUtc,
    long LatestEntitlementVersion,
    LocalServerHeartbeatResponse? LatestHeartbeat,
    ControlCloudInstallationEntitlementStatusResponse? LatestEntitlement,
    ControlCloudEntitlementSyncStatusResponse EntitlementSync,
    ControlCloudInstallationCommandStatusResponse CommandStatus,
    ControlCloudEntitlementReconciliationResponse? Reconciliation = null);

public sealed record ControlCloudEntitlementSyncStatusResponse(
    long? DesiredVersion,
    long? SignedVersion,
    long? ObservedVersion,
    string State,
    string Detail);

public sealed record ControlCloudInstallationEntitlementStatusResponse(
    Guid BundleIssueId,
    long EntitlementVersion,
    Guid EntitlementSnapshotId,
    Guid ClientAccessRevisionId,
    long ContractRevisionNumber,
    Guid ProductCatalogRevisionId,
    long ProductCatalogRevisionNumber,
    DateTimeOffset IssuedAtUtc,
    DateOnly PaidUntil,
    DateOnly WarningStartsAt,
    DateOnly GraceUntil,
    DateOnly OfflineValidUntil,
    string KeyId,
    string PayloadSha256,
    int? AllowedNamedUsers = null,
    int? AllowedConcurrentUsers = null,
    int FeatureLimitCount = 0,
    DateTimeOffset? EffectiveFromUtc = null);

public sealed record ControlCloudEntitlementStateValuesResponse(
    long EntitlementVersion,
    DateTimeOffset EffectiveFromUtc,
    string Status,
    DateOnly PaidUntil,
    DateOnly? WarningStartsAt,
    DateOnly GraceUntil,
    DateOnly OfflineValidUntil,
    int? AllowedDevices,
    int? AllowedBranches,
    int? AllowedNamedUsers,
    int? AllowedConcurrentUsers,
    IReadOnlyCollection<ControlCloudEntitlementStateModuleResponse> Modules,
    IReadOnlyCollection<ControlCloudEntitlementStateFeatureLimitResponse> FeatureLimits);

public sealed record ControlCloudEntitlementStateModuleResponse(
    string ModuleCode,
    bool IsEnabled);

public sealed record ControlCloudEntitlementStateFeatureLimitResponse(
    string ModuleCode,
    string FeatureCode,
    long LimitValue,
    string Unit);

public sealed record ControlCloudEntitlementReconciliationResponse(
    DateTimeOffset EvaluatedAtUtc,
    string State,
    string Detail,
    ControlCloudEntitlementStateValuesResponse? Desired,
    ControlCloudEntitlementStateValuesResponse? Delivered,
    ControlCloudEntitlementStateValuesResponse? Observed,
    IReadOnlyCollection<ControlCloudEntitlementDifferenceResponse> Differences);

public sealed record ControlCloudEntitlementDifferenceResponse(
    string Field,
    string? DesiredValue,
    string? DeliveredValue,
    string? ObservedValue,
    string State,
    string Detail);

public sealed record ControlCloudInstallationCommandStatusResponse(
    int PendingCommandCount,
    long LatestCommandVersion,
    Guid? LatestCommandId,
    string? LatestCommandType,
    string? LatestCommandStatus,
    DateTimeOffset? LatestCommandQueuedAtUtc,
    DateTimeOffset? LatestCommandAcknowledgedAtUtc,
    string? LatestAcknowledgementStatus,
    string? LatestAcknowledgementDetail);
