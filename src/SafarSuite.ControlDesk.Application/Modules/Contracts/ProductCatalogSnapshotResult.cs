using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts;

public sealed record ProductCatalogSnapshotResult(
    string State,
    Guid? CatalogRevisionId,
    long? RevisionNumber,
    Guid? SupersedesCatalogRevisionId,
    Guid? DraftId,
    Guid? BaseCatalogRevisionId,
    long? BaseCatalogRevisionNumber,
    string ChangeReason,
    string ChangedBy,
    DateTimeOffset ChangedAtUtc,
    IReadOnlyCollection<CatalogProductModuleResult> Modules,
    IReadOnlyCollection<CatalogProductModuleGroupResult> ModuleGroups,
    IReadOnlyCollection<CatalogProductResourceResult> Resources);

public sealed record CatalogProductModuleResult(
    string ModuleCode,
    string DisplayName,
    string Description,
    string CommercialMode,
    bool IsActive,
    CatalogProductModuleBillingDefaultsResult? BillingDefaults,
    CatalogProductModuleCompatibilityResult Compatibility);

public sealed record CatalogProductModuleBillingDefaultsResult(
    string ChargeCode,
    string ChargeName,
    string Description,
    decimal DefaultUnitPriceAmount,
    string CurrencyCode,
    string BillingCycle);

public sealed record CatalogProductModuleCompatibilityResult(
    string? MinimumSafarSuiteVersion,
    string? MinimumLocalServerVersion,
    IReadOnlyCollection<string> SupportedDeploymentModes);

public sealed record CatalogProductModuleGroupResult(
    string GroupId,
    string DisplayName,
    string AccessKind,
    IReadOnlyCollection<string> ModuleCodes);

public sealed record CatalogProductResourceResult(
    string ResourceId,
    string DisplayName,
    string AccessKind,
    IReadOnlyCollection<string> RequiredGroupIds,
    IReadOnlyCollection<string> RequiredModuleCodes,
    IReadOnlyCollection<string> ResolvedModuleCodes);

internal static class ProductCatalogSnapshotResultMapper
{
    public static ProductCatalogSnapshotResult FromPublished(ProductCatalogRevision revision)
    {
        return Map(
            "Published",
            revision.Id.Value,
            revision.RevisionNumber,
            revision.SupersedesRevisionId?.Value,
            draftId: null,
            baseCatalogRevisionId: null,
            baseCatalogRevisionNumber: null,
            revision.ChangeReason,
            revision.PublishedBy,
            revision.PublishedAtUtc,
            revision.Definition);
    }

    public static ProductCatalogSnapshotResult FromDraft(ProductCatalogDraft draft)
    {
        return Map(
            "Draft",
            catalogRevisionId: null,
            revisionNumber: null,
            supersedesCatalogRevisionId: null,
            draft.DraftId,
            draft.BaseRevisionId.Value,
            draft.BaseRevisionNumber,
            draft.ChangeReason,
            draft.UpdatedBy,
            draft.UpdatedAtUtc,
            draft.Definition);
    }

    private static ProductCatalogSnapshotResult Map(
        string state,
        Guid? catalogRevisionId,
        long? revisionNumber,
        Guid? supersedesCatalogRevisionId,
        Guid? draftId,
        Guid? baseCatalogRevisionId,
        long? baseCatalogRevisionNumber,
        string changeReason,
        string changedBy,
        DateTimeOffset changedAtUtc,
        ProductCatalogDefinition definition)
    {
        return new ProductCatalogSnapshotResult(
            state,
            catalogRevisionId,
            revisionNumber,
            supersedesCatalogRevisionId,
            draftId,
            baseCatalogRevisionId,
            baseCatalogRevisionNumber,
            changeReason,
            changedBy,
            changedAtUtc,
            definition.Modules.Select(module => new CatalogProductModuleResult(
                    module.ModuleCode.Value,
                    module.DisplayName,
                    module.Description,
                    module.CommercialMode.ToString(),
                    module.IsActive,
                    module.BillingDefaults is null
                        ? null
                        : new CatalogProductModuleBillingDefaultsResult(
                            module.BillingDefaults.ChargeCode,
                            module.BillingDefaults.ChargeName,
                            module.BillingDefaults.Description,
                            module.BillingDefaults.DefaultUnitPriceAmount,
                            module.BillingDefaults.CurrencyCode,
                            module.BillingDefaults.BillingCycle.ToString()),
                    new CatalogProductModuleCompatibilityResult(
                        module.Compatibility.MinimumSafarSuiteVersion,
                        module.Compatibility.MinimumLocalServerVersion,
                        module.Compatibility.SupportedDeploymentModes.ToArray())))
                .ToArray(),
            definition.AccessCatalog.ModuleGroups.Select(group => new CatalogProductModuleGroupResult(
                    group.GroupId,
                    group.DisplayName,
                    group.AccessKind,
                    group.ModuleCodes.ToArray()))
                .ToArray(),
            definition.AccessCatalog.Resources.Select(resource => new CatalogProductResourceResult(
                    resource.ResourceId,
                    resource.DisplayName,
                    resource.AccessKind,
                    resource.RequiredGroupIds.ToArray(),
                    resource.RequiredModuleCodes.ToArray(),
                    resource.ResolvedModuleCodes.ToArray()))
                .ToArray());
    }
}
