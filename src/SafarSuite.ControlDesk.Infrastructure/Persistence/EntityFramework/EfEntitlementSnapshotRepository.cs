using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Entitlements;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfEntitlementSnapshotRepository : IEntitlementSnapshotRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfEntitlementSnapshotRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(EntitlementSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await _dbContext.EntitlementSnapshots.AddAsync(snapshot, cancellationToken);
    }

    public async Task<EntitlementSnapshot?> GetLatestForClientAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.EntitlementSnapshots
            .Include(snapshot => snapshot.Modules)
            .Include(snapshot => snapshot.FeatureLimits)
            .Where(snapshot => snapshot.ClientId == clientId)
            .OrderByDescending(snapshot => snapshot.EntitlementVersion)
            .ThenByDescending(snapshot => snapshot.IssuedAtUtc)
            .ThenByDescending(snapshot => snapshot.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
