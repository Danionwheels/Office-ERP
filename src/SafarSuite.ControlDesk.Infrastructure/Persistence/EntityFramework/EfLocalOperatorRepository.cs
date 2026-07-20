using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Auth.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Auth;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfLocalOperatorRepository(ControlDeskDbContext dbContext)
    : ILocalOperatorRepository
{
    private const string AdministratorMutationLockName =
        "safarsuite:control-desk:local-operator-administrator-mutation:v1";

    public async Task AddAsync(
        LocalOperator localOperator,
        CancellationToken cancellationToken = default)
    {
        await dbContext.LocalOperators.AddAsync(localOperator, cancellationToken);
    }

    public async Task<LocalOperator?> GetByIdAsync(
        LocalOperatorId id,
        CancellationToken cancellationToken = default)
    {
        return await AggregateQuery()
            .SingleOrDefaultAsync(localOperator => localOperator.Id == id, cancellationToken);
    }

    public async Task<LocalOperator?> GetByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        return await AggregateQuery()
            .SingleOrDefaultAsync(
                localOperator => localOperator.NormalizedEmail == normalizedEmail,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<LocalOperator>> ListByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        return await AggregateQuery()
            .Where(localOperator => localOperator.NormalizedEmail == normalizedEmail)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.LocalOperators.AnyAsync(
            localOperator => localOperator.NormalizedEmail == normalizedEmail,
            cancellationToken);
    }

    public async Task AcquireAdministratorMutationLockAsync(
        CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "The local-operator Administrator mutation lock requires an active database transaction.");
        }

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({AdministratorMutationLockName}, 0));",
            cancellationToken);
    }

    public async Task<bool> HasOtherActiveAdministratorAsync(
        LocalOperatorId excludedOperatorId,
        CancellationToken cancellationToken = default)
    {
        return await BuildOtherActiveAdministratorQuery(excludedOperatorId)
            .AnyAsync(cancellationToken);
    }

    internal IQueryable<LocalOperator> BuildOtherActiveAdministratorQuery(
        LocalOperatorId excludedOperatorId)
    {
        return dbContext.LocalOperators.Where(
            localOperator => localOperator.Id != excludedOperatorId
                && localOperator.Status == LocalOperatorStatus.Active
                && localOperator.RoleGrants.Any(grant =>
                    grant.Value == LocalOperatorRole.Administrator)
                && localOperator.ScopeGrants.Any(grant =>
                    grant.Value == LocalOperatorScope.Admin));
    }

    private IQueryable<LocalOperator> AggregateQuery() =>
        dbContext.LocalOperators
            .Include(localOperator => localOperator.RoleGrants)
            .Include(localOperator => localOperator.ScopeGrants)
            .AsSplitQuery();
}
