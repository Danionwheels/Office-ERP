namespace SafarSuite.ControlDesk.Application.Modules.Entitlements.GetLatestEntitlementSnapshot;

public sealed record GetLatestEntitlementSnapshotResult(
    Guid EntitlementSnapshotId,
    Guid ClientId,
    Guid ContractId,
    long ContractRevisionNumber,
    Guid ProductCatalogRevisionId,
    long ProductCatalogRevisionNumber,
    Guid ClientAccessRevisionId,
    long EntitlementVersion,
    string Status,
    DateOnly PaidUntil,
    DateOnly GraceUntil,
    DateOnly OfflineValidUntil,
    int AllowedDevices,
    int AllowedBranches,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset EffectiveFromUtc,
    Guid? SupersedesClientAccessRevisionId,
    string ApprovedBy,
    string ApprovalReason,
    DateTimeOffset ApprovedAtUtc,
    IReadOnlyCollection<GetLatestEntitlementSnapshotModuleResult> Modules,
    int? AllowedNamedUsers = null,
    int? AllowedConcurrentUsers = null,
    IReadOnlyCollection<GetLatestEntitlementSnapshotFeatureLimitResult>? FeatureLimits = null);

public sealed record GetLatestEntitlementSnapshotModuleResult(
    string ModuleCode,
    bool IsEnabled);

public sealed record GetLatestEntitlementSnapshotFeatureLimitResult(
    string ModuleCode,
    string FeatureCode,
    long LimitValue,
    string Unit);
