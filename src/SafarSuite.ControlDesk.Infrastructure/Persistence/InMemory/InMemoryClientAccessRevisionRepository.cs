using SafarSuite.ControlDesk.Application.Modules.Entitlements.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Entitlements;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryClientAccessRevisionRepository : IClientAccessRevisionRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, ClientAccessRevision> _revisionsById = new();

    public Task AddAsync(ClientAccessRevision revision, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (_revisionsById.ContainsKey(revision.Id.Value))
            {
                throw new InvalidOperationException("Client access revision already exists.");
            }

            var latest = FindLatest(revision.ClientId);

            if (latest is null && revision.SupersedesRevisionId is not null)
            {
                throw new InvalidOperationException("The first client access revision cannot supersede another revision.");
            }

            if (latest is not null && revision.SupersedesRevisionId != latest.Id)
            {
                throw new InvalidOperationException("Client access revision must supersede the current latest revision.");
            }

            if (latest is not null && revision.RevisionNumber <= latest.RevisionNumber)
            {
                throw new InvalidOperationException("Client access revision number must increase.");
            }

            _revisionsById.Add(revision.Id.Value, revision);
        }

        return Task.CompletedTask;
    }

    public Task<ClientAccessRevision?> GetByIdAsync(
        ClientAccessRevisionId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            _revisionsById.TryGetValue(id.Value, out var revision);
            return Task.FromResult(revision);
        }
    }

    public Task<ClientAccessRevision?> GetLatestForClientAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            return Task.FromResult(FindLatest(clientId));
        }
    }

    public Task<ClientAccessRevision?> GetLatestForClientForUpdateAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default)
    {
        return GetLatestForClientAsync(clientId, cancellationToken);
    }

    private ClientAccessRevision? FindLatest(ClientId clientId)
    {
        return _revisionsById.Values
            .Where(revision => revision.ClientId == clientId)
            .OrderByDescending(revision => revision.RevisionNumber)
            .ThenByDescending(revision => revision.ApprovedAtUtc)
            .ThenByDescending(revision => revision.Id.Value)
            .FirstOrDefault();
    }
}
