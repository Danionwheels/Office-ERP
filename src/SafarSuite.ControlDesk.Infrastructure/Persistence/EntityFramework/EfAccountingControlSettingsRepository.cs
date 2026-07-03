using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfAccountingControlSettingsRepository : IAccountingControlSettingsRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfAccountingControlSettingsRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AccountingControlSettings settings, CancellationToken cancellationToken = default)
    {
        await _dbContext.AccountingControlSettings.AddAsync(settings, cancellationToken);
    }

    public async Task<AccountingControlSettings?> GetByCompanyAsync(
        string companyCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompanyCode = companyCode.Trim().ToUpperInvariant();

        return await _dbContext.AccountingControlSettings
            .SingleOrDefaultAsync(settings => settings.CompanyCode == normalizedCompanyCode, cancellationToken);
    }
}
