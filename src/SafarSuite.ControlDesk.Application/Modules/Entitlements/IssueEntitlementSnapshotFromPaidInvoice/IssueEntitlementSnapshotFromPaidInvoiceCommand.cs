namespace SafarSuite.ControlDesk.Application.Modules.Entitlements.IssueEntitlementSnapshotFromPaidInvoice;

public sealed record IssueEntitlementSnapshotFromPaidInvoiceCommand(
    Guid InvoiceId,
    DateOnly PaidUntil,
    DateOnly GraceUntil,
    DateOnly OfflineValidUntil,
    int AllowedDevices,
    int AllowedBranches,
    string ApprovedBy,
    string ApprovalReason,
    IReadOnlyCollection<IssueEntitlementSnapshotModuleCommand> Modules,
    int? AllowedNamedUsers = null,
    int? AllowedConcurrentUsers = null,
    IReadOnlyCollection<IssueEntitlementSnapshotFeatureLimitCommand>? FeatureLimits = null,
    DateTimeOffset? EffectiveFromUtc = null);

public sealed record IssueEntitlementSnapshotModuleCommand(
    string ModuleCode,
    bool IsEnabled);

public sealed record IssueEntitlementSnapshotFeatureLimitCommand(
    string ModuleCode,
    string FeatureCode,
    long LimitValue,
    string Unit);
