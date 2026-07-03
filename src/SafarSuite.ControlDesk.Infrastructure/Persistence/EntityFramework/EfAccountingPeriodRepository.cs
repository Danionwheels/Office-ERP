using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfAccountingPeriodRepository : IAccountingPeriodRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfAccountingPeriodRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AccountingPeriod period, CancellationToken cancellationToken = default)
    {
        await _dbContext.AccountingPeriods.AddAsync(period, cancellationToken);
    }

    public async Task<AccountingPeriod?> GetByIdAsync(
        AccountingPeriodId id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AccountingPeriods
            .SingleOrDefaultAsync(period => period.Id == id, cancellationToken);
    }

    public async Task<AccountingPeriod?> GetByCompanyAndStartDateAsync(
        string companyCode,
        DateOnly startsOn,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompanyCode = NormalizeCompanyCode(companyCode);

        return await _dbContext.AccountingPeriods
            .SingleOrDefaultAsync(
                period => period.CompanyCode == normalizedCompanyCode && period.StartsOn == startsOn,
                cancellationToken);
    }

    public async Task<AccountingPeriod?> GetContainingDateAsync(
        string companyCode,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompanyCode = NormalizeCompanyCode(companyCode);

        return await _dbContext.AccountingPeriods
            .SingleOrDefaultAsync(
                period => period.CompanyCode == normalizedCompanyCode
                    && period.StartsOn <= date
                    && period.EndsOn >= date,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<AccountingPeriod>> ListByCompanyAsync(
        string companyCode,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompanyCode = NormalizeCompanyCode(companyCode);
        var periods = _dbContext.AccountingPeriods
            .Where(period => period.CompanyCode == normalizedCompanyCode);

        if (fromDate.HasValue)
        {
            periods = periods.Where(period => period.EndsOn >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            periods = periods.Where(period => period.StartsOn <= toDate.Value);
        }

        return await periods
            .OrderByDescending(period => period.StartsOn)
            .ThenBy(period => period.Id)
            .ToArrayAsync(cancellationToken);
    }

    private static string NormalizeCompanyCode(string companyCode)
    {
        return companyCode.Trim().ToUpperInvariant();
    }
}
