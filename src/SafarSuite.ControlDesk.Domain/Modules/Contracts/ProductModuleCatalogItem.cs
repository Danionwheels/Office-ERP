namespace SafarSuite.ControlDesk.Domain.Modules.Contracts;

public sealed class ProductModuleCatalogItem
{
    private ProductModuleCatalogItem(
        ModuleCode moduleCode,
        string displayName,
        ProductModuleCommercialMode commercialMode,
        bool isActive,
        ProductModuleBillingDefaults? billingDefaults)
    {
        ModuleCode = moduleCode;
        DisplayName = displayName;
        CommercialMode = commercialMode;
        IsActive = isActive;
        BillingDefaults = billingDefaults;
    }

    public ModuleCode ModuleCode { get; }

    public string DisplayName { get; }

    public ProductModuleCommercialMode CommercialMode { get; }

    public bool IsActive { get; }

    public ProductModuleBillingDefaults? BillingDefaults { get; }

    public static ProductModuleCatalogItem Create(
        string moduleCode,
        string displayName,
        ProductModuleCommercialMode commercialMode,
        bool isActive,
        ProductModuleBillingDefaults? billingDefaults = null)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Module display name is required.", nameof(displayName));
        }

        var cleanedDisplayName = displayName.Trim();

        if (cleanedDisplayName.Length > 128)
        {
            throw new ArgumentException("Module display name cannot exceed 128 characters.", nameof(displayName));
        }

        if (!Enum.IsDefined(commercialMode))
        {
            throw new ArgumentException("Module commercial mode is invalid.", nameof(commercialMode));
        }

        return new ProductModuleCatalogItem(
            ModuleCode.Create(moduleCode),
            cleanedDisplayName,
            commercialMode,
            isActive,
            billingDefaults);
    }
}
