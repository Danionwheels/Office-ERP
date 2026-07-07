namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Contracts;

public sealed record ListProductModulesResponse(
    IReadOnlyCollection<ProductModuleResponse> Modules);

public sealed record ProductModuleResponse(
    string ModuleCode,
    string DisplayName,
    string CommercialMode,
    bool IsActive,
    ProductModuleBillingDefaultsResponse? BillingDefaults);

public sealed record ProductModuleBillingDefaultsResponse(
    string ChargeCode,
    string ChargeName,
    string Description,
    decimal DefaultUnitPriceAmount,
    string CurrencyCode,
    string BillingCycle);

public sealed record ProductAccessCatalogResponse(
    IReadOnlyCollection<ProductModuleGroupResponse> ModuleGroups,
    IReadOnlyCollection<ProductResourceResponse> Resources);

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
    string RequestedBy);

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
