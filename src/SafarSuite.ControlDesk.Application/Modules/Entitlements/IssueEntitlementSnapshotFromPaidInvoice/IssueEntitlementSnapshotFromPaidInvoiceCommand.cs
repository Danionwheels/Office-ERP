namespace SafarSuite.ControlDesk.Application.Modules.Entitlements.IssueEntitlementSnapshotFromPaidInvoice;

public sealed record IssueEntitlementSnapshotFromPaidInvoiceCommand(
    Guid InvoiceId,
    DateOnly PaidUntil,
    DateOnly GraceUntil,
    DateOnly OfflineValidUntil,
    int AllowedDevices,
    int AllowedBranches,
    IReadOnlyCollection<IssueEntitlementSnapshotModuleCommand> Modules);

public sealed record IssueEntitlementSnapshotModuleCommand(
    string ModuleCode,
    bool IsEnabled);
