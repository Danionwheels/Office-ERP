using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Infrastructure.ProductModules;

public sealed class ConfiguredProductModuleCatalog
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

    public async Task<ProductCatalogDefinition> GetDefinitionAsync(
        CancellationToken cancellationToken = default)
    {
        var modules = await ListAsync(cancellationToken);
        var accessCatalog = await GetAccessCatalogAsync(cancellationToken);

        return ProductCatalogDefinition.Create(modules, accessCatalog);
    }

    public Task<ProductAccessCatalog> GetAccessCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(BuildAccessCatalog(_options.CurrentValue.AccessCatalog));
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
                BuildBillingDefaults(entry),
                ProductModuleCompatibility.Create(
                    entry.Compatibility.MinimumSafarSuiteVersion,
                    entry.Compatibility.MinimumLocalServerVersion,
                    entry.Compatibility.SupportedDeploymentModes),
                entry.Description);

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

    private static ProductAccessCatalog BuildAccessCatalog(
        ProductAccessCatalogOptions? options)
    {
        var groupEntries = options?.ModuleGroups;
        var resourceEntries = options?.Resources;

        var groups = groupEntries is null || groupEntries.Count == 0
            ? CreateDefaultModuleGroups()
            : groupEntries.Select(BuildModuleGroup).ToArray();
        var groupLookup = new Dictionary<string, ProductModuleGroupCatalogItem>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            if (!groupLookup.TryAdd(group.GroupId, group))
            {
                throw new InvalidOperationException(
                    $"Product access catalog contains duplicate group id {group.GroupId}.");
            }
        }

        var resources = resourceEntries is null || resourceEntries.Count == 0
            ? CreateDefaultResources(groupLookup)
            : resourceEntries.Select(resource => BuildResource(resource, groupLookup)).ToArray();
        var resourceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var resource in resources)
        {
            if (!resourceIds.Add(resource.ResourceId))
            {
                throw new InvalidOperationException(
                    $"Product access catalog contains duplicate resource id {resource.ResourceId}.");
            }
        }

        return new ProductAccessCatalog(groups, resources);
    }

    private static ProductModuleGroupCatalogItem BuildModuleGroup(
        ProductModuleGroupOptions entry)
    {
        var groupId = NormalizeRequiredText(
            entry.GroupId,
            "Product access catalog group id is required.");
        var displayName = NormalizeRequiredText(
            entry.DisplayName,
            $"Product access catalog group {groupId} display name is required.");
        var moduleCodes = NormalizeIds(entry.ModuleCodes);

        if (moduleCodes.Count == 0)
        {
            throw new InvalidOperationException(
                $"Product access catalog group {groupId} must include at least one module code.");
        }

        return new ProductModuleGroupCatalogItem(
            groupId,
            displayName,
            NormalizeAccessKind(entry.AccessKind),
            moduleCodes);
    }

    private static ProductResourceCatalogItem BuildResource(
        ProductResourceOptions entry,
        IReadOnlyDictionary<string, ProductModuleGroupCatalogItem> groupLookup)
    {
        var resourceId = NormalizeRequiredText(
            entry.ResourceId,
            "Product access catalog resource id is required.");
        var displayName = NormalizeRequiredText(
            entry.DisplayName,
            $"Product access catalog resource {resourceId} display name is required.");
        var requiredGroupIds = NormalizeIds(entry.RequiredGroupIds);
        var requiredModuleCodes = NormalizeIds(entry.RequiredModuleCodes);

        return BuildResource(
            resourceId,
            displayName,
            NormalizeAccessKind(entry.AccessKind),
            requiredGroupIds,
            requiredModuleCodes,
            groupLookup);
    }

    private static ProductResourceCatalogItem BuildResource(
        string resourceId,
        string displayName,
        string accessKind,
        IReadOnlyCollection<string> requiredGroupIds,
        IReadOnlyCollection<string> requiredModuleCodes,
        IReadOnlyDictionary<string, ProductModuleGroupCatalogItem> groupLookup)
    {
        var resolvedModuleCodes = new List<string>();
        var resolvedLookup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var moduleCode in requiredModuleCodes)
        {
            if (resolvedLookup.Add(moduleCode))
            {
                resolvedModuleCodes.Add(moduleCode);
            }
        }

        foreach (var groupId in requiredGroupIds)
        {
            if (!groupLookup.TryGetValue(groupId, out var group))
            {
                throw new InvalidOperationException(
                    $"Product access catalog resource {resourceId} references unknown group {groupId}.");
            }

            foreach (var moduleCode in group.ModuleCodes)
            {
                if (resolvedLookup.Add(moduleCode))
                {
                    resolvedModuleCodes.Add(moduleCode);
                }
            }
        }

        if (!string.Equals(accessKind, ProductAccessKinds.Public, StringComparison.Ordinal)
            && resolvedModuleCodes.Count == 0)
        {
            throw new InvalidOperationException(
                $"Product access catalog resource {resourceId} must resolve to at least one module code.");
        }

        return new ProductResourceCatalogItem(
            resourceId,
            displayName,
            accessKind,
            requiredGroupIds,
            requiredModuleCodes,
            resolvedModuleCodes);
    }

    private static IReadOnlyCollection<ProductModuleGroupCatalogItem> CreateDefaultModuleGroups()
    {
        return new[]
        {
            Group("foundation-core", "Foundation Core", ProductAccessKinds.CoreIncluded,
                "platform",
                "identity-access",
                "tenant-branch",
                "module-registry",
                "entitlements",
                "notifications",
                "audit"),
            Group("accounting-ledger", "Accounting Ledger", ProductAccessKinds.PaidModule,
                "accounting"),
            Group("reporting", "Reporting", ProductAccessKinds.PaidModule,
                "reporting-core"),
            Group("clients-parties", "Clients & Parties", ProductAccessKinds.PaidModule,
                "clients-parties"),
            Group("travel", "Travel", ProductAccessKinds.PaidModule,
                "travel",
                "ticket-stock"),
            Group("tour", "Tour", ProductAccessKinds.PaidModule,
                "tour",
                "visa",
                "hotels",
                "transport"),
            Group("connectivity", "Connectivity", ProductAccessKinds.PaidModule,
                "cloud-sync",
                "owner-cloud-dashboard",
                "cloud-backup",
                "cloud-consolidated-reports",
                "remote-monitoring")
        };
    }

    private static IReadOnlyCollection<ProductResourceCatalogItem> CreateDefaultResources(
        IReadOnlyDictionary<string, ProductModuleGroupCatalogItem> groupLookup)
    {
        return new[]
        {
            Resource(
                ProductResourceIds.ProductKernelState,
                "Product Kernel State",
                ProductAccessKinds.Public,
                [],
                [],
                groupLookup),
            Resource(
                ProductResourceIds.ProductKernelModules,
                "Module Administration",
                ProductAccessKinds.CoreIncluded,
                ["foundation-core"],
                [],
                groupLookup),
            Resource(
                ProductResourceIds.ReportsCatalog,
                "Report Catalog",
                ProductAccessKinds.PaidModule,
                ["reporting"],
                [],
                groupLookup),
            Resource(
                ProductResourceIds.ReportsExecute,
                "Report Execution",
                ProductAccessKinds.PaidModule,
                ["reporting"],
                [],
                groupLookup),
            Resource(
                ProductResourceIds.ReportsAudit,
                "Report Audit",
                ProductAccessKinds.PaidModule,
                ["reporting"],
                [],
                groupLookup),
            Resource(
                ProductResourceIds.AccountingWrite,
                "Accounting Writes",
                ProductAccessKinds.PaidModule,
                ["accounting-ledger"],
                [],
                groupLookup)
        };
    }

    private static ProductModuleGroupCatalogItem Group(
        string groupId,
        string displayName,
        string accessKind,
        params string[] moduleCodes)
    {
        return new ProductModuleGroupCatalogItem(
            groupId,
            displayName,
            accessKind,
            moduleCodes);
    }

    private static ProductResourceCatalogItem Resource(
        string resourceId,
        string displayName,
        string accessKind,
        IReadOnlyCollection<string> requiredGroupIds,
        IReadOnlyCollection<string> requiredModuleCodes,
        IReadOnlyDictionary<string, ProductModuleGroupCatalogItem> groupLookup)
    {
        return BuildResource(
            resourceId,
            displayName,
            accessKind,
            requiredGroupIds,
            requiredModuleCodes,
            groupLookup);
    }

    private static IReadOnlyCollection<string> NormalizeIds(
        IEnumerable<string>? values)
    {
        var ids = new List<string>();
        var lookup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values ?? Array.Empty<string>())
        {
            var normalized = value.Trim();
            if (normalized.Length == 0)
            {
                continue;
            }

            if (lookup.Add(normalized))
            {
                ids.Add(normalized);
            }
        }

        return ids;
    }

    private static string NormalizeRequiredText(string value, string errorMessage)
    {
        var normalized = value.Trim();
        if (normalized.Length == 0)
        {
            throw new InvalidOperationException(errorMessage);
        }

        return normalized;
    }

    private static string NormalizeAccessKind(string accessKind)
    {
        if (string.IsNullOrWhiteSpace(accessKind))
        {
            return ProductAccessKinds.PaidModule;
        }

        return accessKind.Trim().ToLowerInvariant() switch
        {
            "public" => ProductAccessKinds.Public,
            "coreincluded" or "core_included" or "core-included" => ProductAccessKinds.CoreIncluded,
            "paidmodule" or "paid_module" or "paid-add-on" or "paid-addon" or "paid-module" => ProductAccessKinds.PaidModule,
            _ => throw new InvalidOperationException(
                $"Product access catalog access kind {accessKind} is not supported.")
        };
    }
}
