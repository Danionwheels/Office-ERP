namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Entitlements;

public sealed record IssueEntitlementSnapshotFromPaidInvoiceRequest(
    Guid InvoiceId,
    DateOnly PaidUntil,
    DateOnly GraceUntil,
    DateOnly OfflineValidUntil,
    int AllowedDevices,
    int AllowedBranches,
    string ApprovalReason,
    IReadOnlyCollection<EntitlementModuleRequest> Modules,
    int? AllowedNamedUsers = null,
    int? AllowedConcurrentUsers = null,
    IReadOnlyCollection<EntitlementFeatureLimitRequest>? FeatureLimits = null,
    DateTimeOffset? EffectiveFromUtc = null);

public sealed record IssueEntitlementSnapshotFromPaidInvoiceDefaultsRequest(
    Guid InvoiceId,
    string ApprovalReason,
    DateTimeOffset? EffectiveFromUtc = null);

public sealed record EntitlementModuleRequest(
    string ModuleCode,
    bool IsEnabled);

public sealed record EntitlementFeatureLimitRequest(
    string ModuleCode,
    string FeatureCode,
    long LimitValue,
    string Unit);

public sealed record IssueEntitlementSnapshotFromPaidInvoiceResponse(
    Guid EntitlementSnapshotId,
    Guid ClientId,
    Guid ContractId,
    long ContractRevisionNumber,
    Guid ProductCatalogRevisionId,
    long ProductCatalogRevisionNumber,
    Guid ClientAccessRevisionId,
    long EntitlementVersion,
    Guid InvoiceId,
    string InvoiceNumber,
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
    IReadOnlyCollection<EntitlementModuleResponse> Modules,
    int? AllowedNamedUsers = null,
    int? AllowedConcurrentUsers = null,
    IReadOnlyCollection<EntitlementFeatureLimitResponse>? FeatureLimits = null);

public sealed record EntitlementSnapshotResponse(
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
    IReadOnlyCollection<EntitlementModuleResponse> Modules,
    int? AllowedNamedUsers = null,
    int? AllowedConcurrentUsers = null,
    IReadOnlyCollection<EntitlementFeatureLimitResponse>? FeatureLimits = null);

public sealed record EntitlementModuleResponse(
    string ModuleCode,
    bool IsEnabled);

public sealed record EntitlementFeatureLimitResponse(
    string ModuleCode,
    string FeatureCode,
    long LimitValue,
    string Unit);
