using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryClientDeploymentRepository : IClientDeploymentRepository
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, ClientDeployment>> _deploymentsByClientId = new();

    public Task AddAsync(ClientDeployment deployment, CancellationToken cancellationToken = default)
    {
        var clientDeployments = _deploymentsByClientId.GetOrAdd(
            deployment.ClientId.Value,
            _ => new ConcurrentDictionary<Guid, ClientDeployment>());

        clientDeployments.TryAdd(deployment.Id.Value, deployment);

        return Task.CompletedTask;
    }

    public Task<ClientDeployment?> GetByClientAndInstallationIdAsync(
        ClientId clientId,
        string installationId,
        CancellationToken cancellationToken = default)
    {
        if (!_deploymentsByClientId.TryGetValue(clientId.Value, out var clientDeployments))
        {
            return Task.FromResult<ClientDeployment?>(null);
        }

        var deployment = clientDeployments.Values.SingleOrDefault(item =>
            item.InstallationId.Equals(installationId.Trim(), StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(deployment);
    }

    public Task<IReadOnlyCollection<ClientDeployment>> ListByClientIdAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default)
    {
        if (!_deploymentsByClientId.TryGetValue(clientId.Value, out var clientDeployments))
        {
            return Task.FromResult<IReadOnlyCollection<ClientDeployment>>([]);
        }

        var deployments = clientDeployments.Values
            .OrderByDescending(deployment => deployment.IsPrimary)
            .ThenBy(deployment => deployment.DisplayName)
            .ThenBy(deployment => deployment.InstallationId)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ClientDeployment>>(deployments);
    }
}
