using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.Ports;

public interface IClientRepository
{
    Task AddAsync(Client client, CancellationToken cancellationToken = default);

    Task<Client?> GetByIdAsync(ClientId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Client>> ListAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeAsync(ClientCode code, CancellationToken cancellationToken = default);
}
