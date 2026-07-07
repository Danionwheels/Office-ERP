using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfOpeningBalanceProfileRepository : IOpeningBalanceProfileRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfOpeningBalanceProfileRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(OpeningBalanceProfile profile, CancellationToken cancellationToken = default)
    {
        await _dbContext.OpeningBalanceProfiles.AddAsync(profile, cancellationToken);
    }

    public async Task<OpeningBalanceProfile?> GetByCompanyAsync(
        string companyCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompanyCode = companyCode.Trim().ToUpperInvariant();

        return await _dbContext.OpeningBalanceProfiles
            .SingleOrDefaultAsync(profile => profile.CompanyCode == normalizedCompanyCode, cancellationToken);
    }
}
