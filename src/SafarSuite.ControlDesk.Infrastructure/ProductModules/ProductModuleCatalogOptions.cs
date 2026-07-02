namespace SafarSuite.ControlDesk.Infrastructure.ProductModules;

public sealed class ProductModuleCatalogOptions
{
    public const string SectionName = "ProductModules";

    public List<ProductModuleCatalogEntryOptions> Modules { get; set; } = [];
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
