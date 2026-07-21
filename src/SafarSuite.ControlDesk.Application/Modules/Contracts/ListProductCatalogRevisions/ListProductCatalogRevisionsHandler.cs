using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts.ListProductCatalogRevisions;

public sealed class ListProductCatalogRevisionsHandler
{
    private readonly IProductModuleCatalog _catalog;

    public ListProductCatalogRevisionsHandler(IProductModuleCatalog catalog)
    {
        _catalog = catalog;
    }

    public async Task<Result<IReadOnlyCollection<ProductCatalogSnapshotResult>>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var revisions = await _catalog.ListPublishedRevisionsAsync(cancellationToken);

        return Result<IReadOnlyCollection<ProductCatalogSnapshotResult>>.Success(
            revisions.Select(ProductCatalogSnapshotResultMapper.FromPublished).ToArray());
    }
}
