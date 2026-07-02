using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Infrastructure.ProductModules;

public sealed class ConfiguredProductModuleCatalog : IProductModuleCatalog
{
    private readonly IOptionsMonitor<ProductModuleCatalogOptions> _options;

    public ConfiguredProductModuleCatalog(IOptionsMonitor<ProductModuleCatalogOptions> options)
    {
        _options = options;
    }

    public Task<IReadOnlyCollection<ProductModuleCatalogItem>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(BuildCatalog(_options.CurrentValue));
    }

    private static IReadOnlyCollection<ProductModuleCatalogItem> BuildCatalog(
        ProductModuleCatalogOptions options)
    {
        var modules = new List<ProductModuleCatalogItem>();
        var moduleCodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in options.Modules)
        {
            var commercialMode = ParseCommercialMode(entry);
            var module = ProductModuleCatalogItem.Create(
                entry.ModuleCode,
                entry.DisplayName,
                commercialMode,
                entry.IsActive,
                BuildBillingDefaults(entry));

            if (!moduleCodes.Add(module.ModuleCode.Value))
            {
                throw new InvalidOperationException(
                    $"Product module catalog contains duplicate module code {module.ModuleCode.Value}.");
            }

            modules.Add(module);
        }

        return modules
            .OrderBy(module => module.CommercialMode)
            .ThenBy(module => module.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(module => module.ModuleCode.Value, StringComparer.Ordinal)
            .ToArray();
    }

    private static ProductModuleCommercialMode ParseCommercialMode(
        ProductModuleCatalogEntryOptions entry)
    {
        if (string.IsNullOrWhiteSpace(entry.CommercialMode))
        {
            return ProductModuleCommercialMode.PaidAddOn;
        }

        if (Enum.TryParse<ProductModuleCommercialMode>(
                entry.CommercialMode.Trim(),
                ignoreCase: true,
                out var commercialMode)
            && Enum.IsDefined(commercialMode))
        {
            return commercialMode;
        }

        throw new InvalidOperationException(
            $"Product module catalog entry {entry.ModuleCode} has unsupported commercial mode {entry.CommercialMode}.");
    }

    private static ProductModuleBillingDefaults? BuildBillingDefaults(
        ProductModuleCatalogEntryOptions entry)
    {
        if (entry.BillingDefaults is null)
        {
            return null;
        }

        var billingCycle = ParseBillingCycle(entry);

        return ProductModuleBillingDefaults.Create(
            entry.BillingDefaults.ChargeCode,
            entry.BillingDefaults.ChargeName,
            entry.BillingDefaults.Description,
            entry.BillingDefaults.DefaultUnitPriceAmount,
            entry.BillingDefaults.CurrencyCode,
            billingCycle);
    }

    private static BillingCycle ParseBillingCycle(ProductModuleCatalogEntryOptions entry)
    {
        if (Enum.TryParse<BillingCycle>(
                entry.BillingDefaults!.BillingCycle.Trim(),
                ignoreCase: true,
                out var billingCycle)
            && Enum.IsDefined(billingCycle))
        {
            return billingCycle;
        }

        throw new InvalidOperationException(
            $"Product module catalog entry {entry.ModuleCode} has unsupported billing cycle {entry.BillingDefaults.BillingCycle}.");
    }
}
