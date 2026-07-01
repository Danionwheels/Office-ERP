namespace SafarSuite.ControlDesk.Application.Modules.Entitlements.IssueEntitlementSnapshotFromPaidInvoice;

public sealed record IssueEntitlementSnapshotFromPaidInvoiceResult(
    Guid EntitlementSnapshotId,
    Guid ClientId,
    Guid ContractId,
    Guid InvoiceId,
    string InvoiceNumber,
    string Status,
    DateOnly PaidUntil,
    DateOnly GraceUntil,
    DateOnly OfflineValidUntil,
    int AllowedDevices,
    int AllowedBranches,
    DateTimeOffset IssuedAtUtc,
    IReadOnlyCollection<IssueEntitlementSnapshotModuleResult> Modules);

public sealed record IssueEntitlementSnapshotModuleResult(
    string ModuleCode,
    bool IsEnabled);
