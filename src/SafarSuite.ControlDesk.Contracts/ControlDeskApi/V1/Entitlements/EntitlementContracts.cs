namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Entitlements;

public sealed record IssueEntitlementSnapshotFromPaidInvoiceRequest(
    Guid InvoiceId,
    DateOnly PaidUntil,
    DateOnly GraceUntil,
    DateOnly OfflineValidUntil,
    int AllowedDevices,
    int AllowedBranches,
    IReadOnlyCollection<EntitlementModuleRequest> Modules);

public sealed record IssueEntitlementSnapshotFromPaidInvoiceDefaultsRequest(
    Guid InvoiceId);

public sealed record EntitlementModuleRequest(
    string ModuleCode,
    bool IsEnabled);

public sealed record IssueEntitlementSnapshotFromPaidInvoiceResponse(
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
    IReadOnlyCollection<EntitlementModuleResponse> Modules);

public sealed record EntitlementSnapshotResponse(
    Guid EntitlementSnapshotId,
    Guid ClientId,
    Guid ContractId,
    string Status,
    DateOnly PaidUntil,
    DateOnly GraceUntil,
    DateOnly OfflineValidUntil,
    int AllowedDevices,
    int AllowedBranches,
    DateTimeOffset IssuedAtUtc,
    IReadOnlyCollection<EntitlementModuleResponse> Modules);

public sealed record EntitlementModuleResponse(
    string ModuleCode,
    bool IsEnabled);
