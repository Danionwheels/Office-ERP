namespace SafarSuite.ControlDesk.Infrastructure.ProductModules;

public sealed class ProductModuleCatalogOptions
{
    public const string SectionName = "ProductModules";

    public List<ProductModuleCatalogEntryOptions> Modules { get; set; } = [];

    public ProductAccessCatalogOptions AccessCatalog { get; set; } = new();
}

public sealed class ProductModuleCatalogEntryOptions
{
    public string ModuleCode { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string CommercialMode { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ProductModuleBillingDefaultsOptions? BillingDefaults { get; set; }
}

public sealed class ProductModuleBillingDefaultsOptions
{
    public string ChargeCode { get; set; } = string.Empty;

    public string ChargeName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal DefaultUnitPriceAmount { get; set; }

    public string CurrencyCode { get; set; } = "PKR";

    public string BillingCycle { get; set; } = "Monthly";
}

public sealed class ProductAccessCatalogOptions
{
    public List<ProductModuleGroupOptions> ModuleGroups { get; set; } = [];

    public List<ProductResourceOptions> Resources { get; set; } = [];
}

public sealed class ProductModuleGroupOptions
{
    public string GroupId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string AccessKind { get; set; } = string.Empty;

    public List<string> ModuleCodes { get; set; } = [];
}

public sealed class ProductResourceOptions
{
    public string ResourceId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string AccessKind { get; set; } = string.Empty;

    public List<string> RequiredGroupIds { get; set; } = [];

    public List<string> RequiredModuleCodes { get; set; } = [];
}
