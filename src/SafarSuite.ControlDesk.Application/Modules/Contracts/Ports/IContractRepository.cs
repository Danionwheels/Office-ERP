using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;

public interface IContractRepository
{
    Task AddAsync(ClientContract contract, CancellationToken cancellationToken = default);

    Task<ClientContract?> GetByIdAsync(ContractId id, CancellationToken cancellationToken = default);

    Task<ClientContract?> GetActiveForClientAsync(ClientId clientId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ClientContract>> ListForClientAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNumberAsync(ContractNumber number, CancellationToken cancellationToken = default);
}
