using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfProviderBankDetailsRepository : IProviderBankDetailsRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfProviderBankDetailsRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ProviderBankDetails?> GetAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.ProviderBankDetails
            .SingleOrDefaultAsync(details => details.Id == ProviderBankDetailsId.Singleton, cancellationToken);
    }

    public async Task AddAsync(ProviderBankDetails details, CancellationToken cancellationToken = default)
    {
        await _dbContext.ProviderBankDetails.AddAsync(details, cancellationToken);
    }
}
