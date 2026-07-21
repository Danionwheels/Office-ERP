using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;

public interface IProductModuleCatalog
{
    Task<ProductCatalogRevision> GetPublishedRevisionAsync(
        CancellationToken cancellationToken = default);

    Task<ProductCatalogDraft?> GetDraftAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProductCatalogRevision>> ListPublishedRevisionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProductModuleCatalogItem>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<ProductAccessCatalog> GetAccessCatalogAsync(
        CancellationToken cancellationToken = default);
}

public interface IProductCatalogRepository
{
    Task<ProductCatalogRevision?> GetLatestPublishedAsync(
        CancellationToken cancellationToken = default);

    Task<ProductCatalogRevision?> GetLatestPublishedForUpdateAsync(
        CancellationToken cancellationToken = default);

    Task<ProductCatalogDraft?> GetDraftAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProductCatalogRevision>> ListPublishedAsync(
        CancellationToken cancellationToken = default);

    Task AddPublishedAsync(
        ProductCatalogRevision revision,
        CancellationToken cancellationToken = default);

    Task SaveDraftAsync(
        ProductCatalogDraft draft,
        CancellationToken cancellationToken = default);

    Task DeleteDraftAsync(
        CancellationToken cancellationToken = default);
}
