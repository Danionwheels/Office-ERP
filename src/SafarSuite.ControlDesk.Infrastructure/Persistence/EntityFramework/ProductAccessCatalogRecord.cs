namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

internal sealed class ProductAccessCatalogRecord
{
    private ProductAccessCatalogRecord()
    {
    }

    public ProductAccessCatalogRecord(
        string catalogId,
        string moduleGroupsJson,
        string resourcesJson,
        DateTimeOffset updatedAtUtc,
        string updatedBy)
    {
        CatalogId = catalogId;
        ModuleGroupsJson = moduleGroupsJson;
        ResourcesJson = resourcesJson;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedBy = updatedBy;
    }

    public string CatalogId { get; private set; } = string.Empty;

    public string ModuleGroupsJson { get; private set; } = "[]";

    public string ResourcesJson { get; private set; } = "[]";

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public string UpdatedBy { get; private set; } = string.Empty;

    public void Replace(
        string moduleGroupsJson,
        string resourcesJson,
        DateTimeOffset updatedAtUtc,
        string updatedBy)
    {
        ModuleGroupsJson = moduleGroupsJson;
        ResourcesJson = resourcesJson;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedBy = updatedBy;
    }
}
