namespace SafarSuite.ControlDesk.Application.Modules.Entitlements.GetLatestEntitlementSnapshot;

public sealed record GetLatestEntitlementSnapshotResult(
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
    IReadOnlyCollection<GetLatestEntitlementSnapshotModuleResult> Modules);

public sealed record GetLatestEntitlementSnapshotModuleResult(
    string ModuleCode,
    bool IsEnabled);
