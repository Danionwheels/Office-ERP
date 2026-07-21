namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Contracts;

public sealed record ListProductModulesResponse(
    IReadOnlyCollection<ProductModuleResponse> Modules,
    Guid? CatalogRevisionId = null,
    long? CatalogRevisionNumber = null);

public sealed record ProductModuleResponse(
    string ModuleCode,
    string DisplayName,
    string CommercialMode,
    bool IsActive,
    ProductModuleBillingDefaultsResponse? BillingDefaults,
    ProductModuleCompatibilityResponse? Compatibility = null,
    string Description = "",
    IReadOnlyCollection<ProductModuleContractReferenceResponse>? ReferencedBy = null);

public sealed record ProductModuleContractReferenceResponse(
    Guid ContractId,
    string ContractNumber,
    long ContractRevisionNumber,
    Guid ClientId);

public sealed record ProductModuleCompatibilityResponse(
    string? MinimumSafarSuiteVersion,
    string? MinimumLocalServerVersion,
    IReadOnlyCollection<string> SupportedDeploymentModes);

public sealed record ProductModuleBillingDefaultsResponse(
    string ChargeCode,
    string ChargeName,
    string Description,
    decimal DefaultUnitPriceAmount,
    string CurrencyCode,
    string BillingCycle);

public sealed record ProductAccessCatalogResponse(
    IReadOnlyCollection<ProductModuleGroupResponse> ModuleGroups,
    IReadOnlyCollection<ProductResourceResponse> Resources,
    IReadOnlyCollection<ProductModuleResponse>? Modules = null,
    string State = "Published",
    Guid? CatalogRevisionId = null,
    long? RevisionNumber = null,
    Guid? SupersedesCatalogRevisionId = null,
    Guid? DraftId = null,
    Guid? BaseCatalogRevisionId = null,
    long? BaseCatalogRevisionNumber = null,
    string ChangeReason = "",
    string ChangedBy = "",
    DateTimeOffset? ChangedAtUtc = null);

public sealed record ListProductCatalogRevisionsResponse(
    IReadOnlyCollection<ProductAccessCatalogResponse> Revisions);

public sealed record ProductModuleGroupResponse(
    string GroupId,
    string DisplayName,
    string AccessKind,
    IReadOnlyCollection<string> ModuleCodes);

public sealed record ProductResourceResponse(
    string ResourceId,
    string DisplayName,
    string AccessKind,
    IReadOnlyCollection<string> RequiredGroupIds,
    IReadOnlyCollection<string> RequiredModuleCodes,
    IReadOnlyCollection<string> ResolvedModuleCodes);

public sealed record PublishProductAccessCatalogCommandRequest(
    Guid ActivationRequestId,
    int? ExpiresInHours,
    string RequestedBy);

public sealed record SaveProductAccessCatalogRequest(
    IReadOnlyCollection<SaveProductModuleGroupRequest> ModuleGroups,
    IReadOnlyCollection<SaveProductResourceRequest> Resources,
    string RequestedBy,
    IReadOnlyCollection<SaveProductModuleRequest>? Modules = null,
    string ChangeReason = "Product catalog definition updated.");

public sealed record SaveProductModuleRequest(
    string ModuleCode,
    string DisplayName,
    string CommercialMode,
    bool IsActive,
    ProductModuleBillingDefaultsResponse? BillingDefaults,
    ProductModuleCompatibilityResponse? Compatibility,
    string Description = "");

public sealed record SaveProductModuleGroupRequest(
    string GroupId,
    string DisplayName,
    string AccessKind,
    IReadOnlyCollection<string> ModuleCodes);

public sealed record SaveProductResourceRequest(
    string ResourceId,
    string DisplayName,
    string AccessKind,
    IReadOnlyCollection<string> RequiredGroupIds,
    IReadOnlyCollection<string> RequiredModuleCodes);

public sealed record PublishProductAccessCatalogCommandResponse(
    Guid CommandId,
    Guid ServerInstallationId,
    string CommandType,
    string ProductKernelCommand,
    string Signature,
    string SigningKeyId,
    DateTimeOffset ExpiresAt,
    ProductAccessCatalogResponse AccessCatalog);

public sealed record PublishProductCatalogRevisionRequest(string RequestedBy);
