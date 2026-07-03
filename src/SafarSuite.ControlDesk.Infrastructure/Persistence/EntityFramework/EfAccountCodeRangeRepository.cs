using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfAccountCodeRangeRepository : IAccountCodeRangeRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfAccountCodeRangeRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AccountCodeRange range, CancellationToken cancellationToken = default)
    {
        await _dbContext.AccountCodeRanges.AddAsync(range, cancellationToken);
    }

    public async Task<bool> AnyByCompanyAsync(string companyCode, CancellationToken cancellationToken = default)
    {
        var normalizedCompanyCode = NormalizeCompanyCode(companyCode);

        return await _dbContext.AccountCodeRanges.AnyAsync(
            range => range.CompanyCode == normalizedCompanyCode,
            cancellationToken);
    }

    public async Task<AccountCodeRange?> GetByCompanyAndRoleAsync(
        string companyCode,
        string role,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompanyCode = NormalizeCompanyCode(companyCode);
        var normalizedRole = role.Trim().ToUpperInvariant();

        return await _dbContext.AccountCodeRanges
            .SingleOrDefaultAsync(
                range => range.CompanyCode == normalizedCompanyCode
                    && range.Role.ToUpper() == normalizedRole,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<AccountCodeRange>> ListByCompanyAsync(
        string companyCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompanyCode = NormalizeCompanyCode(companyCode);

        return await _dbContext.AccountCodeRanges
            .AsNoTracking()
            .Where(range => range.CompanyCode == normalizedCompanyCode)
            .OrderBy(range => range.Role)
            .ToArrayAsync(cancellationToken);
    }

    private static string NormalizeCompanyCode(string companyCode)
    {
        return companyCode.Trim().ToUpperInvariant();
    }
}
