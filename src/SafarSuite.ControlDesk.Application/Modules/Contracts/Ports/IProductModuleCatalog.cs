using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;

public interface IProductModuleCatalog
{
    Task<IReadOnlyCollection<ProductModuleCatalogItem>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<ProductAccessCatalog> GetAccessCatalogAsync(
        CancellationToken cancellationToken = default);
}

public interface IProductAccessCatalogRepository
{
    Task<ProductAccessCatalog?> GetAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(
        ProductAccessCatalog catalog,
        string updatedBy,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken = default);
}
