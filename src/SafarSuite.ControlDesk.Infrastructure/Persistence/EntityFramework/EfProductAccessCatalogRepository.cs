using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfProductAccessCatalogRepository : IProductAccessCatalogRepository
{
    private const string DefaultCatalogId = "default";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ControlDeskDbContext _dbContext;

    public EfProductAccessCatalogRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductAccessCatalog?> GetAsync(CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<ProductAccessCatalogRecord>()
            .SingleOrDefaultAsync(catalog => catalog.CatalogId == DefaultCatalogId, cancellationToken);

        if (record is null)
        {
            return null;
        }

        var groups = Deserialize<ProductModuleGroupCatalogItem>(record.ModuleGroupsJson);
        var resources = Deserialize<ProductResourceCatalogItem>(record.ResourcesJson);

        return new ProductAccessCatalog(groups, resources);
    }

    public async Task SaveAsync(
        ProductAccessCatalog catalog,
        string updatedBy,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var moduleGroupsJson = JsonSerializer.Serialize(catalog.ModuleGroups.ToArray(), JsonOptions);
        var resourcesJson = JsonSerializer.Serialize(catalog.Resources.ToArray(), JsonOptions);
        var record = await _dbContext.Set<ProductAccessCatalogRecord>()
            .SingleOrDefaultAsync(item => item.CatalogId == DefaultCatalogId, cancellationToken);

        if (record is null)
        {
            await _dbContext.Set<ProductAccessCatalogRecord>().AddAsync(
                new ProductAccessCatalogRecord(
                    DefaultCatalogId,
                    moduleGroupsJson,
                    resourcesJson,
                    updatedAtUtc,
                    updatedBy),
                cancellationToken);

            return;
        }

        record.Replace(moduleGroupsJson, resourcesJson, updatedAtUtc, updatedBy);
    }

    private static IReadOnlyCollection<T> Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T[]>(json, JsonOptions) ?? [];
    }
}
