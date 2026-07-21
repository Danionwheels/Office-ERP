using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

internal sealed class ProductAccessCatalogRecord
{
    private ProductAccessCatalogRecord()
    {
    }

    public ProductAccessCatalogRecord(
        string catalogId,
        Guid draftId,
        ProductCatalogRevisionId baseCatalogRevisionId,
        long baseCatalogRevisionNumber,
        string modulesJson,
        string moduleGroupsJson,
        string resourcesJson,
        string changeReason,
        DateTimeOffset updatedAtUtc,
        string updatedBy)
    {
        CatalogId = catalogId;
        DraftId = draftId;
        BaseCatalogRevisionId = baseCatalogRevisionId;
        BaseCatalogRevisionNumber = baseCatalogRevisionNumber;
        ModulesJson = modulesJson;
        ModuleGroupsJson = moduleGroupsJson;
        ResourcesJson = resourcesJson;
        ChangeReason = changeReason;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedBy = updatedBy;
    }

    public string CatalogId { get; private set; } = string.Empty;

    public Guid DraftId { get; private set; }

    public ProductCatalogRevisionId BaseCatalogRevisionId { get; private set; }

    public long BaseCatalogRevisionNumber { get; private set; }

    public string ModulesJson { get; private set; } = "[]";

    public string ModuleGroupsJson { get; private set; } = "[]";

    public string ResourcesJson { get; private set; } = "[]";

    public string ChangeReason { get; private set; } = string.Empty;

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public string UpdatedBy { get; private set; } = string.Empty;

    public void Replace(
        Guid draftId,
        ProductCatalogRevisionId baseCatalogRevisionId,
        long baseCatalogRevisionNumber,
        string modulesJson,
        string moduleGroupsJson,
        string resourcesJson,
        string changeReason,
        DateTimeOffset updatedAtUtc,
        string updatedBy)
    {
        DraftId = draftId;
        BaseCatalogRevisionId = baseCatalogRevisionId;
        BaseCatalogRevisionNumber = baseCatalogRevisionNumber;
        ModulesJson = modulesJson;
        ModuleGroupsJson = moduleGroupsJson;
        ResourcesJson = resourcesJson;
        ChangeReason = changeReason;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedBy = updatedBy;
    }
}
