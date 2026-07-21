namespace SafarSuite.ControlDesk.Domain.Modules.Contracts;

public static class ProductAccessKinds
{
    public const string Public = "Public";
    public const string CoreIncluded = "CoreIncluded";
    public const string PaidModule = "PaidModule";
}

public static class ProductResourceIds
{
    public const string ProductKernelState = "product-kernel.state";
    public const string ProductKernelModules = "product-kernel.modules";
    public const string ReportsCatalog = "reports.catalog";
    public const string ReportsExecute = "reports.execute";
    public const string ReportsAudit = "reports.audit";
    public const string AccountingWrite = "accounting.write";
}

public sealed record ProductAccessCatalog(
    IReadOnlyCollection<ProductModuleGroupCatalogItem> ModuleGroups,
    IReadOnlyCollection<ProductResourceCatalogItem> Resources);

public sealed record ProductModuleGroupCatalogItem(
    string GroupId,
    string DisplayName,
    string AccessKind,
    IReadOnlyCollection<string> ModuleCodes);

public sealed record ProductResourceCatalogItem(
    string ResourceId,
    string DisplayName,
    string AccessKind,
    IReadOnlyCollection<string> RequiredGroupIds,
    IReadOnlyCollection<string> RequiredModuleCodes,
    IReadOnlyCollection<string> ResolvedModuleCodes);
