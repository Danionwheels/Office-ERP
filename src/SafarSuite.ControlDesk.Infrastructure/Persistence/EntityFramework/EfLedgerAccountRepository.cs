using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfLedgerAccountRepository : ILedgerAccountRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfLedgerAccountRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(LedgerAccount ledgerAccount, CancellationToken cancellationToken = default)
    {
        await _dbContext.LedgerAccounts.AddAsync(ledgerAccount, cancellationToken);
    }

    public async Task<LedgerAccount?> GetByIdAsync(LedgerAccountId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LedgerAccounts
            .SingleOrDefaultAsync(account => account.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(LedgerAccountCode code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LedgerAccounts.AnyAsync(account => account.Code == code, cancellationToken);
    }
}
