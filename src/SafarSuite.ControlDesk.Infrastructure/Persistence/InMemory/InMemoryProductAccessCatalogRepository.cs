using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryProductAccessCatalogRepository : IProductAccessCatalogRepository
{
    private readonly object _gate = new();
    private ProductAccessCatalog? _catalog;

    public Task<ProductAccessCatalog?> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return Task.FromResult(_catalog);
        }
    }

    public Task SaveAsync(
        ProductAccessCatalog catalog,
        string updatedBy,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _catalog = catalog;
        }

        return Task.CompletedTask;
    }
}
