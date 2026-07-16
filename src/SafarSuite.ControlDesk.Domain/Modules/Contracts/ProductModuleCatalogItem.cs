namespace SafarSuite.ControlDesk.Domain.Modules.Contracts;

public sealed class ProductModuleCatalogItem
{
    private ProductModuleCatalogItem(
        ModuleCode moduleCode,
        string displayName,
        string description,
        ProductModuleCommercialMode commercialMode,
        bool isActive,
        ProductModuleBillingDefaults? billingDefaults,
        ProductModuleCompatibility compatibility)
    {
        ModuleCode = moduleCode;
        DisplayName = displayName;
        Description = description;
        CommercialMode = commercialMode;
        IsActive = isActive;
        BillingDefaults = billingDefaults;
        Compatibility = compatibility;
    }

    public ModuleCode ModuleCode { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public ProductModuleCommercialMode CommercialMode { get; }

    public bool IsActive { get; }

    public ProductModuleBillingDefaults? BillingDefaults { get; }

    public ProductModuleCompatibility Compatibility { get; }

    public static ProductModuleCatalogItem Create(
        string moduleCode,
        string displayName,
        ProductModuleCommercialMode commercialMode,
        bool isActive,
        ProductModuleBillingDefaults? billingDefaults = null,
        ProductModuleCompatibility? compatibility = null,
        string? description = null)
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

        var cleanedDescription = description?.Trim() ?? string.Empty;

        if (cleanedDescription.Length > 1000)
        {
            throw new ArgumentException("Module description cannot exceed 1000 characters.", nameof(description));
        }

        return new ProductModuleCatalogItem(
            ModuleCode.Create(moduleCode),
            cleanedDisplayName,
            cleanedDescription,
            commercialMode,
            isActive,
            billingDefaults,
            compatibility ?? ProductModuleCompatibility.Any);
    }
}
