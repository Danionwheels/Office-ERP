using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Entitlements;

namespace SafarSuite.ControlDesk.Application.Modules.Entitlements.Ports;

public interface IClientAccessRevisionRepository
{
    Task AddAsync(ClientAccessRevision revision, CancellationToken cancellationToken = default);

    Task<ClientAccessRevision?> GetByIdAsync(
        ClientAccessRevisionId id,
        CancellationToken cancellationToken = default);

    Task<ClientAccessRevision?> GetLatestForClientAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default);

    Task<ClientAccessRevision?> GetLatestForClientForUpdateAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default);
}
