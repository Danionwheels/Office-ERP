using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfContractRepository : IContractRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfContractRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ClientContract contract, CancellationToken cancellationToken = default)
    {
        await _dbContext.ClientContracts.AddAsync(contract, cancellationToken);
    }

    public async Task<ClientContract?> GetByIdAsync(ContractId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClientContracts
            .Include(contract => contract.ModuleAllowances)
            .Include(contract => contract.FeatureLimits)
            .SingleOrDefaultAsync(contract => contract.Id == id, cancellationToken);
    }

    public async Task<ClientContract?> GetActiveForClientAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClientContracts
            .Include(contract => contract.ModuleAllowances)
            .Include(contract => contract.FeatureLimits)
            .Where(contract => contract.ClientId == clientId)
            .Where(contract => contract.Status == ContractStatus.Active)
            .OrderByDescending(contract => contract.ActivatedAtUtc)
            .ThenByDescending(contract => contract.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ClientContract?> GetLatestForClientForUpdateAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({clientId.Value + ":contract"}, 0));",
            cancellationToken);

        return await _dbContext.ClientContracts
            .Include(contract => contract.ModuleAllowances)
            .Include(contract => contract.FeatureLimits)
            .Where(contract => contract.ClientId == clientId)
            .OrderByDescending(contract => contract.RevisionNumber)
            .ThenByDescending(contract => contract.ApprovedAtUtc)
            .ThenByDescending(contract => contract.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ClientContract>> ListForClientAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClientContracts
            .Include(contract => contract.ModuleAllowances)
            .Include(contract => contract.FeatureLimits)
            .Where(contract => contract.ClientId == clientId)
            .OrderByDescending(contract => contract.RevisionNumber)
            .ThenByDescending(contract => contract.ApprovedAtUtc)
            .ThenByDescending(contract => contract.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNumberAsync(
        ContractNumber number,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClientContracts
            .AnyAsync(contract => contract.Number == number, cancellationToken);
    }
}
