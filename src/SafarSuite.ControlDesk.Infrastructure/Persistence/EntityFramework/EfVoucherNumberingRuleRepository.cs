using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfVoucherNumberingRuleRepository : IVoucherNumberingRuleRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfVoucherNumberingRuleRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(VoucherNumberingRule rule, CancellationToken cancellationToken = default)
    {
        await _dbContext.VoucherNumberingRules.AddAsync(rule, cancellationToken);
    }

    public async Task<VoucherNumberingRule?> GetByCompanyAndSourceTypeAsync(
        string companyCode,
        JournalSourceType sourceType,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompanyCode = companyCode.Trim().ToUpperInvariant();

        return await _dbContext.VoucherNumberingRules
            .SingleOrDefaultAsync(
                rule => rule.CompanyCode == normalizedCompanyCode && rule.SourceType == sourceType,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<VoucherNumberingRule>> ListByCompanyAsync(
        string companyCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompanyCode = companyCode.Trim().ToUpperInvariant();

        return await _dbContext.VoucherNumberingRules
            .Where(rule => rule.CompanyCode == normalizedCompanyCode)
            .OrderBy(rule => rule.SourceType)
            .ToArrayAsync(cancellationToken);
    }
}
