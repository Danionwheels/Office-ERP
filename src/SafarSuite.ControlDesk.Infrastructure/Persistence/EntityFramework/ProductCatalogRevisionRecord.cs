using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

internal sealed class ProductCatalogRevisionRecord
{
    private ProductCatalogRevisionRecord()
    {
    }

    public ProductCatalogRevisionRecord(
        ProductCatalogRevisionId catalogRevisionId,
        long revisionNumber,
        ProductCatalogRevisionId? supersedesCatalogRevisionId,
        string modulesJson,
        string moduleGroupsJson,
        string resourcesJson,
        string changeReason,
        string publishedBy,
        DateTimeOffset publishedAtUtc)
    {
        CatalogRevisionId = catalogRevisionId;
        RevisionNumber = revisionNumber;
        SupersedesCatalogRevisionId = supersedesCatalogRevisionId;
        ModulesJson = modulesJson;
        ModuleGroupsJson = moduleGroupsJson;
        ResourcesJson = resourcesJson;
        ChangeReason = changeReason;
        PublishedBy = publishedBy;
        PublishedAtUtc = publishedAtUtc;
    }

    public ProductCatalogRevisionId CatalogRevisionId { get; private set; }

    public long RevisionNumber { get; private set; }

    public ProductCatalogRevisionId? SupersedesCatalogRevisionId { get; private set; }

    public string ModulesJson { get; private set; } = "[]";

    public string ModuleGroupsJson { get; private set; } = "[]";

    public string ResourcesJson { get; private set; } = "[]";

    public string ChangeReason { get; private set; } = string.Empty;

    public string PublishedBy { get; private set; } = string.Empty;

    public DateTimeOffset PublishedAtUtc { get; private set; }
}
