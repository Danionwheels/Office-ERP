using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Entitlements;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryEntitlementSnapshotRepository : IEntitlementSnapshotRepository
{
    private readonly ConcurrentDictionary<Guid, EntitlementSnapshot> _snapshotsById = new();

    public Task AddAsync(EntitlementSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        _snapshotsById.TryAdd(snapshot.Id.Value, snapshot);

        return Task.CompletedTask;
    }

    public Task<EntitlementSnapshot?> GetLatestForClientAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = _snapshotsById.Values
            .Where(snapshot => snapshot.ClientId == clientId)
            .OrderByDescending(snapshot => snapshot.EntitlementVersion)
            .ThenByDescending(snapshot => snapshot.IssuedAtUtc)
            .ThenByDescending(snapshot => snapshot.Id.Value)
            .FirstOrDefault();

        return Task.FromResult(snapshot);
    }
}
