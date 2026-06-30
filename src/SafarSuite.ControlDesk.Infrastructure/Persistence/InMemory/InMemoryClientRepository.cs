using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryClientRepository : IClientRepository
{
    private readonly ConcurrentDictionary<Guid, Client> _clientsById = new();

    public Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        _clientsById.TryAdd(client.Id.Value, client);

        return Task.CompletedTask;
    }

    public Task<Client?> GetByIdAsync(ClientId id, CancellationToken cancellationToken = default)
    {
        _clientsById.TryGetValue(id.Value, out var client);

        return Task.FromResult(client);
    }

    public Task<IReadOnlyCollection<Client>> ListAsync(CancellationToken cancellationToken = default)
    {
        var clients = _clientsById.Values
            .OrderBy(client => client.Code.Value)
            .ThenBy(client => client.DisplayName)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<Client>>(clients);
    }

    public Task<bool> ExistsByCodeAsync(ClientCode code, CancellationToken cancellationToken = default)
    {
        var exists = _clientsById.Values.Any(client => client.Code.Equals(code));

        return Task.FromResult(exists);
    }
}
