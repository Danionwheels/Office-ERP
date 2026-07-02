using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;

public interface IProductModuleCatalog
{
    Task<IReadOnlyCollection<ProductModuleCatalogItem>> ListAsync(
        CancellationToken cancellationToken = default);
}
