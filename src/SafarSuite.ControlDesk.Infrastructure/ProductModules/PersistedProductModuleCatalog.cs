using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Infrastructure.ProductModules;

public sealed class PersistedProductModuleCatalog : IProductModuleCatalog
{
    private readonly ConfiguredProductModuleCatalog _configuredCatalog;
    private readonly IProductAccessCatalogRepository _repository;

    public PersistedProductModuleCatalog(
        ConfiguredProductModuleCatalog configuredCatalog,
        IProductAccessCatalogRepository repository)
    {
        _configuredCatalog = configuredCatalog;
        _repository = repository;
    }

    public Task<IReadOnlyCollection<ProductModuleCatalogItem>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return _configuredCatalog.ListAsync(cancellationToken);
    }

    public async Task<ProductAccessCatalog> GetAccessCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var persistedCatalog = await _repository.GetAsync(cancellationToken);

        return persistedCatalog
            ?? await _configuredCatalog.GetAccessCatalogAsync(cancellationToken);
    }
}
