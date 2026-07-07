using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts.ListProductAccessCatalog;

public sealed class ListProductAccessCatalogHandler
{
    private readonly IProductModuleCatalog _catalog;

    public ListProductAccessCatalogHandler(IProductModuleCatalog catalog)
    {
        _catalog = catalog;
    }

    public async Task<Result<ListProductAccessCatalogResult>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var catalog = await _catalog.GetAccessCatalogAsync(cancellationToken);

        return Result<ListProductAccessCatalogResult>.Success(new ListProductAccessCatalogResult(
            catalog.ModuleGroups.Select(group => new ProductModuleGroupResult(
                    group.GroupId,
                    group.DisplayName,
                    group.AccessKind,
                    group.ModuleCodes.ToArray()))
                .ToArray(),
            catalog.Resources.Select(resource => new ProductResourceResult(
                    resource.ResourceId,
                    resource.DisplayName,
                    resource.AccessKind,
                    resource.RequiredGroupIds.ToArray(),
                    resource.RequiredModuleCodes.ToArray(),
                    resource.ResolvedModuleCodes.ToArray()))
                .ToArray()));
    }
}

public sealed record ListProductAccessCatalogResult(
    IReadOnlyCollection<ProductModuleGroupResult> ModuleGroups,
    IReadOnlyCollection<ProductResourceResult> Resources);

public sealed record ProductModuleGroupResult(
    string GroupId,
    string DisplayName,
    string AccessKind,
    IReadOnlyCollection<string> ModuleCodes);

public sealed record ProductResourceResult(
    string ResourceId,
    string DisplayName,
    string AccessKind,
    IReadOnlyCollection<string> RequiredGroupIds,
    IReadOnlyCollection<string> RequiredModuleCodes,
    IReadOnlyCollection<string> ResolvedModuleCodes);
