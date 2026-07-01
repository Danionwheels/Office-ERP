using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Billing;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfChargeCodeRepository : IChargeCodeRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfChargeCodeRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ChargeCode chargeCode, CancellationToken cancellationToken = default)
    {
        await _dbContext.ChargeCodes.AddAsync(chargeCode, cancellationToken);
    }

    public async Task<ChargeCode?> GetByIdAsync(ChargeCodeId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChargeCodes
            .SingleOrDefaultAsync(chargeCode => chargeCode.Id == id, cancellationToken);
    }

    public async Task<ChargeCode?> GetByCodeAsync(ChargeCodeKey code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChargeCodes
            .SingleOrDefaultAsync(chargeCode => chargeCode.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ChargeCode>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChargeCodes
            .AsNoTracking()
            .OrderBy(chargeCode => chargeCode.Code)
            .ThenBy(chargeCode => chargeCode.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(ChargeCodeKey code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChargeCodes
            .AnyAsync(chargeCode => chargeCode.Code == code, cancellationToken);
    }
}
