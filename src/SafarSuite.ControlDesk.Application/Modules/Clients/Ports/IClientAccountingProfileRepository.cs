using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.Ports;

public interface IClientAccountingProfileRepository
{
    Task AddAsync(ClientAccountingProfile profile, CancellationToken cancellationToken = default);

    Task<ClientAccountingProfile?> GetByClientIdAsync(ClientId clientId, CancellationToken cancellationToken = default);
}
