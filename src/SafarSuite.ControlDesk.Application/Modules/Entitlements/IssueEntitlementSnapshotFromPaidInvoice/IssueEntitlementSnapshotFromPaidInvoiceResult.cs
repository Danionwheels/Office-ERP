namespace SafarSuite.ControlDesk.Application.Modules.Entitlements.IssueEntitlementSnapshotFromPaidInvoice;

public sealed record IssueEntitlementSnapshotFromPaidInvoiceResult(
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
    IReadOnlyCollection<IssueEntitlementSnapshotModuleResult> Modules,
    int? AllowedNamedUsers = null,
    int? AllowedConcurrentUsers = null,
    IReadOnlyCollection<IssueEntitlementSnapshotFeatureLimitResult>? FeatureLimits = null);

public sealed record IssueEntitlementSnapshotModuleResult(
    string ModuleCode,
    bool IsEnabled);

public sealed record IssueEntitlementSnapshotFeatureLimitResult(
    string ModuleCode,
    string FeatureCode,
    long LimitValue,
    string Unit);
