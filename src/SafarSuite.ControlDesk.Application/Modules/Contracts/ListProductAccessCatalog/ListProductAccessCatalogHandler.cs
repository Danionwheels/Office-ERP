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

    public async Task<Result<ProductCatalogSnapshotResult>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var draft = await _catalog.GetDraftAsync(cancellationToken);

        if (draft is not null)
        {
            return Result<ProductCatalogSnapshotResult>.Success(
                ProductCatalogSnapshotResultMapper.FromDraft(draft));
        }

        var published = await _catalog.GetPublishedRevisionAsync(cancellationToken);

        return Result<ProductCatalogSnapshotResult>.Success(
            ProductCatalogSnapshotResultMapper.FromPublished(published));
    }
}
