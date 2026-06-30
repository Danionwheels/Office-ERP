using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Entitlements;

namespace SafarSuite.ControlDesk.Application.Modules.Entitlements.Ports;

public interface IEntitlementSnapshotRepository
{
    Task AddAsync(EntitlementSnapshot snapshot, CancellationToken cancellationToken = default);

    Task<EntitlementSnapshot?> GetLatestForClientAsync(ClientId clientId, CancellationToken cancellationToken = default);
}
