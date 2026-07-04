namespace SafarSuite.ControlDesk.Contracts.SafarSuiteApp.V1;

public static class SafarSuiteProductKernelCommandTypes
{
    public const string SetProductAccessCatalog = "SetProductAccessCatalog";
}

public sealed record IssueProductKernelCommandRequest(
    string CommandType,
    string? ModuleId,
    bool? IsEnabled,
    string? EntitlementStatus,
    DateOnly? PaidUntil,
    DateOnly? GraceEndsOn,
    DateOnly? OfflineValidUntil,
    IReadOnlyDictionary<string, bool>? ModuleEntitlements,
    DateTimeOffset? ExpiresAt = null,
    ProductAccessCatalogCommandPayload? AccessCatalog = null);

public sealed record CloudProductKernelCommandResponse(
    Guid CommandId,
    Guid ServerInstallationId,
    string CommandType,
    string? ModuleId,
    bool? IsEnabled,
    string? EntitlementStatus,
    string ProductKernelCommand,
    string Signature,
    string SigningKeyId,
    DateTimeOffset ExpiresAt);

public sealed record ProductAccessCatalogCommandPayload(
    IReadOnlyCollection<ProductModuleGroupCommandPayload> ModuleGroups,
    IReadOnlyCollection<ProductResourceCommandPayload> Resources);

public sealed record ProductModuleGroupCommandPayload(
    string GroupId,
    string DisplayName,
    string AccessKind,
    IReadOnlyCollection<string> ModuleIds);

public sealed record ProductResourceCommandPayload(
    string ResourceId,
    string DisplayName,
    string AccessKind,
    IReadOnlyCollection<string> RequiredGroupIds,
    IReadOnlyCollection<string> RequiredModuleIds,
    IReadOnlyCollection<string>? ResolvedModuleIds = null);
