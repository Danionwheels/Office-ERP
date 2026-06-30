using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryClientAccountingProfileRepository : IClientAccountingProfileRepository
{
    private readonly ConcurrentDictionary<Guid, ClientAccountingProfile> _profilesByClientId = new();

    public Task AddAsync(ClientAccountingProfile profile, CancellationToken cancellationToken = default)
    {
        _profilesByClientId.TryAdd(profile.ClientId.Value, profile);

        return Task.CompletedTask;
    }

    public Task<ClientAccountingProfile?> GetByClientIdAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default)
    {
        _profilesByClientId.TryGetValue(clientId.Value, out var profile);

        return Task.FromResult(profile);
    }
}
