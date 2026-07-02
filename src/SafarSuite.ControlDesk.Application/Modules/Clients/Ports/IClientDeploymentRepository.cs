using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.Ports;

public interface IClientDeploymentRepository
{
    Task AddAsync(ClientDeployment deployment, CancellationToken cancellationToken = default);

    Task<ClientDeployment?> GetByClientAndInstallationIdAsync(
        ClientId clientId,
        string installationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ClientDeployment>> ListByClientIdAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default);
}
