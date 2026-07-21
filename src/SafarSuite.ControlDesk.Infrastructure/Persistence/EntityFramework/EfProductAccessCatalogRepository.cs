using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfProductAccessCatalogRepository : IProductCatalogRepository
{
    private const string DefaultCatalogId = "default";
    private const string CatalogLockName = "product-catalog:publish";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ControlDeskDbContext _dbContext;

    public EfProductAccessCatalogRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductCatalogRevision?> GetLatestPublishedAsync(
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<ProductCatalogRevisionRecord>()
            .AsNoTracking()
            .OrderByDescending(revision => revision.RevisionNumber)
            .ThenByDescending(revision => revision.PublishedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    public async Task<ProductCatalogRevision?> GetLatestPublishedForUpdateAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({CatalogLockName}, 0));",
            cancellationToken);

        var record = await _dbContext.Set<ProductCatalogRevisionRecord>()
            .AsNoTracking()
            .OrderByDescending(revision => revision.RevisionNumber)
            .ThenByDescending(revision => revision.PublishedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    public async Task<ProductCatalogDraft?> GetDraftAsync(
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<ProductAccessCatalogRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(catalog => catalog.CatalogId == DefaultCatalogId, cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyCollection<ProductCatalogRevision>> ListPublishedAsync(
        CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.Set<ProductCatalogRevisionRecord>()
            .AsNoTracking()
            .OrderByDescending(revision => revision.RevisionNumber)
            .ThenByDescending(revision => revision.PublishedAtUtc)
            .ToArrayAsync(cancellationToken);

        return records.Select(ToDomain).ToArray();
    }

    public async Task AddPublishedAsync(
        ProductCatalogRevision revision,
        CancellationToken cancellationToken = default)
    {
        var definitionJson = Serialize(revision.Definition);

        await _dbContext.Set<ProductCatalogRevisionRecord>().AddAsync(
            new ProductCatalogRevisionRecord(
                revision.Id,
                revision.RevisionNumber,
                revision.SupersedesRevisionId,
                definitionJson.Modules,
                definitionJson.ModuleGroups,
                definitionJson.Resources,
                revision.ChangeReason,
                revision.PublishedBy,
                revision.PublishedAtUtc),
            cancellationToken);
    }

    public async Task SaveDraftAsync(
        ProductCatalogDraft draft,
        CancellationToken cancellationToken = default)
    {
        var definitionJson = Serialize(draft.Definition);
        var record = await _dbContext.Set<ProductAccessCatalogRecord>()
            .SingleOrDefaultAsync(catalog => catalog.CatalogId == DefaultCatalogId, cancellationToken);

        if (record is null)
        {
            await _dbContext.Set<ProductAccessCatalogRecord>().AddAsync(
                new ProductAccessCatalogRecord(
                    DefaultCatalogId,
                    draft.DraftId,
                    draft.BaseRevisionId,
                    draft.BaseRevisionNumber,
                    definitionJson.Modules,
                    definitionJson.ModuleGroups,
                    definitionJson.Resources,
                    draft.ChangeReason,
                    draft.UpdatedAtUtc,
                    draft.UpdatedBy),
                cancellationToken);

            return;
        }

        record.Replace(
            draft.DraftId,
            draft.BaseRevisionId,
            draft.BaseRevisionNumber,
            definitionJson.Modules,
            definitionJson.ModuleGroups,
            definitionJson.Resources,
            draft.ChangeReason,
            draft.UpdatedAtUtc,
            draft.UpdatedBy);
    }

    public async Task DeleteDraftAsync(CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<ProductAccessCatalogRecord>()
            .SingleOrDefaultAsync(catalog => catalog.CatalogId == DefaultCatalogId, cancellationToken);

        if (record is not null)
        {
            _dbContext.Set<ProductAccessCatalogRecord>().Remove(record);
        }
    }

    private static ProductCatalogRevision ToDomain(ProductCatalogRevisionRecord record)
    {
        return ProductCatalogRevision.Publish(
            record.CatalogRevisionId,
            record.RevisionNumber,
            record.SupersedesCatalogRevisionId,
            DeserializeDefinition(record.ModulesJson, record.ModuleGroupsJson, record.ResourcesJson),
            record.ChangeReason,
            record.PublishedBy,
            record.PublishedAtUtc);
    }

    private static ProductCatalogDraft ToDomain(ProductAccessCatalogRecord record)
    {
        return ProductCatalogDraft.Save(
            record.DraftId,
            record.BaseCatalogRevisionId,
            record.BaseCatalogRevisionNumber,
            DeserializeDefinition(record.ModulesJson, record.ModuleGroupsJson, record.ResourcesJson),
            record.ChangeReason,
            record.UpdatedBy,
            record.UpdatedAtUtc);
    }

    private static ProductCatalogDefinition DeserializeDefinition(
        string modulesJson,
        string moduleGroupsJson,
        string resourcesJson)
    {
        var storedModules = Deserialize<StoredProductModule>(modulesJson);
        var modules = storedModules.Select(ToDomain).ToArray();
        var groups = Deserialize<ProductModuleGroupCatalogItem>(moduleGroupsJson);
        var resources = Deserialize<ProductResourceCatalogItem>(resourcesJson);

        return ProductCatalogDefinition.Create(
            modules,
            new ProductAccessCatalog(groups, resources));
    }

    private static ProductModuleCatalogItem ToDomain(StoredProductModule module)
    {
        ProductModuleBillingDefaults? billingDefaults = null;

        if (module.BillingDefaults is not null)
        {
            if (!Enum.TryParse<BillingCycle>(
                    module.BillingDefaults.BillingCycle,
                    ignoreCase: true,
                    out var billingCycle)
                || !Enum.IsDefined(billingCycle))
            {
                throw new InvalidOperationException(
                    $"Stored product module {module.ModuleCode} has an invalid billing cycle.");
            }

            billingDefaults = ProductModuleBillingDefaults.Create(
                module.BillingDefaults.ChargeCode,
                module.BillingDefaults.ChargeName,
                module.BillingDefaults.Description,
                module.BillingDefaults.DefaultUnitPriceAmount,
                module.BillingDefaults.CurrencyCode,
                billingCycle);
        }

        if (!Enum.TryParse<ProductModuleCommercialMode>(
                module.CommercialMode,
                ignoreCase: true,
                out var commercialMode)
            || !Enum.IsDefined(commercialMode))
        {
            throw new InvalidOperationException(
                $"Stored product module {module.ModuleCode} has an invalid commercial mode.");
        }

        return ProductModuleCatalogItem.Create(
            module.ModuleCode,
            module.DisplayName,
            commercialMode,
            module.IsActive,
            billingDefaults,
            ProductModuleCompatibility.Create(
                module.Compatibility?.MinimumSafarSuiteVersion,
                module.Compatibility?.MinimumLocalServerVersion,
                module.Compatibility?.SupportedDeploymentModes),
            module.Description);
    }

    private static ProductCatalogDefinitionJson Serialize(ProductCatalogDefinition definition)
    {
        var modules = definition.Modules.Select(module => new StoredProductModule(
                module.ModuleCode.Value,
                module.DisplayName,
                module.CommercialMode.ToString(),
                module.IsActive,
                module.BillingDefaults is null
                    ? null
                    : new StoredProductModuleBillingDefaults(
                        module.BillingDefaults.ChargeCode,
                        module.BillingDefaults.ChargeName,
                        module.BillingDefaults.Description,
                        module.BillingDefaults.DefaultUnitPriceAmount,
                        module.BillingDefaults.CurrencyCode,
                        module.BillingDefaults.BillingCycle.ToString()),
                new StoredProductModuleCompatibility(
                    module.Compatibility.MinimumSafarSuiteVersion,
                    module.Compatibility.MinimumLocalServerVersion,
                    module.Compatibility.SupportedDeploymentModes.ToArray()),
                module.Description))
            .ToArray();

        return new ProductCatalogDefinitionJson(
            JsonSerializer.Serialize(modules, JsonOptions),
            JsonSerializer.Serialize(definition.AccessCatalog.ModuleGroups.ToArray(), JsonOptions),
            JsonSerializer.Serialize(definition.AccessCatalog.Resources.ToArray(), JsonOptions));
    }

    private static IReadOnlyCollection<T> Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T[]>(json, JsonOptions) ?? [];
    }

    private sealed record ProductCatalogDefinitionJson(
        string Modules,
        string ModuleGroups,
        string Resources);

    private sealed record StoredProductModule(
        string ModuleCode,
        string DisplayName,
        string CommercialMode,
        bool IsActive,
        StoredProductModuleBillingDefaults? BillingDefaults,
        StoredProductModuleCompatibility? Compatibility,
        string? Description = null);

    private sealed record StoredProductModuleBillingDefaults(
        string ChargeCode,
        string ChargeName,
        string Description,
        decimal DefaultUnitPriceAmount,
        string CurrencyCode,
        string BillingCycle);

    private sealed record StoredProductModuleCompatibility(
        string? MinimumSafarSuiteVersion,
        string? MinimumLocalServerVersion,
        IReadOnlyCollection<string> SupportedDeploymentModes);
}
