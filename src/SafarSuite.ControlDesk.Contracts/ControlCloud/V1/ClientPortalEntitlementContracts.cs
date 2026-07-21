namespace SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

public sealed record ClientPortalSignedEntitlementBundleResponse(
    string PayloadJson,
    ClientPortalEntitlementBundlePayloadResponse Payload,
    ClientPortalEntitlementBundleSignatureResponse Signature);

public sealed record ClientPortalEntitlementBundlePayloadResponse(
    string BundleVersion,
    string Issuer,
    string Audience,
    Guid ClientId,
    string InstallationId,
    long EntitlementVersion,
    Guid BundleIssueId,
    Guid EntitlementSnapshotId,
    Guid ClientAccessRevisionId,
    Guid ContractId,
    long ContractRevisionNumber,
    Guid ProductCatalogRevisionId,
    long ProductCatalogRevisionNumber,
    Guid SourceInvoiceId,
    string SourceInvoiceNumber,
    string Status,
    DateTimeOffset BundleIssuedAtUtc,
    DateTimeOffset EntitlementIssuedAtUtc,
    DateOnly ValidFrom,
    DateOnly PaidUntil,
    DateOnly WarningStartsAt,
    DateOnly GraceUntil,
    DateOnly OfflineValidUntil,
    int AllowedDevices,
    int AllowedBranches,
    IReadOnlyCollection<ClientPortalEntitlementBundleModuleResponse> Modules,
    int? AllowedNamedUsers = null,
    int? AllowedConcurrentUsers = null,
    IReadOnlyCollection<ClientPortalEntitlementBundleFeatureLimitResponse>? FeatureLimits = null,
    DateTimeOffset? EffectiveFromUtc = null);

public sealed record ClientPortalEntitlementBundleModuleResponse(
    string ModuleCode,
    string Status,
    bool IsEnabled);

public sealed record ClientPortalEntitlementBundleFeatureLimitResponse(
    string ModuleCode,
    string FeatureCode,
    long LimitValue,
    string Unit);

public sealed record ClientPortalEntitlementBundleSignatureResponse(
    string Algorithm,
    string KeyId,
    string PayloadSha256,
    string Value);
