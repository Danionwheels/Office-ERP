using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Entitlements;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfClientAccessRevisionRepository : IClientAccessRevisionRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfClientAccessRevisionRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ClientAccessRevision revision, CancellationToken cancellationToken = default)
    {
        await _dbContext.ClientAccessRevisions.AddAsync(revision, cancellationToken);
    }

    public async Task<ClientAccessRevision?> GetByIdAsync(
        ClientAccessRevisionId id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClientAccessRevisions
            .Include(revision => revision.Modules)
            .Include(revision => revision.FeatureLimits)
            .SingleOrDefaultAsync(revision => revision.Id == id, cancellationToken);
    }

    public async Task<ClientAccessRevision?> GetLatestForClientAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default)
    {
        return await LatestForClientQuery(clientId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ClientAccessRevision?> GetLatestForClientForUpdateAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({clientId.Value.ToString()}, 0));",
            cancellationToken);

        return await LatestForClientQuery(clientId).FirstOrDefaultAsync(cancellationToken);
    }

    private IOrderedQueryable<ClientAccessRevision> LatestForClientQuery(ClientId clientId)
    {
        return _dbContext.ClientAccessRevisions
            .Include(revision => revision.Modules)
            .Include(revision => revision.FeatureLimits)
            .Where(revision => revision.ClientId == clientId)
            .OrderByDescending(revision => revision.RevisionNumber)
            .ThenByDescending(revision => revision.ApprovedAtUtc)
            .ThenByDescending(revision => revision.Id);
    }
}
