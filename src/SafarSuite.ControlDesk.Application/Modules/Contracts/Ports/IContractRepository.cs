using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;

public interface IContractRepository
{
    Task AddAsync(ClientContract contract, CancellationToken cancellationToken = default);

    Task<ClientContract?> GetByIdAsync(ContractId id, CancellationToken cancellationToken = default);

    Task<ClientContract?> GetActiveForClientAsync(ClientId clientId, CancellationToken cancellationToken = default);
}
