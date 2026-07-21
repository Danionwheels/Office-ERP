using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfProductModuleReferenceReader : IProductModuleReferenceReader
{
    private readonly ControlDeskDbContext _dbContext;

    public EfProductModuleReferenceReader(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<ProductModuleContractReference>> ListActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var contracts = await _dbContext.ClientContracts
            .AsNoTracking()
            .Include(contract => contract.ModuleAllowances)
            .Where(contract => contract.Status == ContractStatus.Active)
            .ToArrayAsync(cancellationToken);

        return contracts
            .SelectMany(contract => contract.ModuleAllowances
                .Where(module => module.IsEnabled)
                .Select(module => new ProductModuleContractReference(
                    module.ModuleCode.Value,
                    contract.Id.Value,
                    contract.Number.Value,
                    contract.RevisionNumber,
                    contract.ClientId.Value)))
            .OrderBy(reference => reference.ContractNumber, StringComparer.OrdinalIgnoreCase)
            .ThenBy(reference => reference.ContractRevisionNumber)
            .ThenBy(reference => reference.ContractId)
            .ToArray();
    }
}
