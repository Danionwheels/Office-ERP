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

    public async Task<LedgerAccount?> GetByCodeAsync(
        LedgerAccountCode code,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.LedgerAccounts
            .SingleOrDefaultAsync(account => account.Code == code, cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(LedgerAccountCode code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LedgerAccounts.AnyAsync(account => account.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyCollection<LedgerAccount>> ListAsync(
        string? search = null,
        LedgerAccountType? type = null,
        LedgerAccountStatus? status = null,
        bool? isPostingAccount = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.LedgerAccounts.AsNoTracking();
        var normalizedSearch = search?.Trim();

        if (type.HasValue)
        {
            query = query.Where(account => account.Type == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(account => account.Status == status.Value);
        }

        if (isPostingAccount.HasValue)
        {
            query = query.Where(account => account.IsPostingAccount == isPostingAccount.Value);
        }

        var accounts = await query.ToArrayAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            accounts = accounts
                .Where(account =>
                    account.Code.Value.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
                    || account.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        return accounts
            .OrderBy(account => account.Code.Value, StringComparer.Ordinal)
            .ThenBy(account => account.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<string>> ListCodesByPrefixAsync(
        string codePrefix,
        CancellationToken cancellationToken = default)
    {
        var normalizedPrefix = codePrefix.Trim().ToUpperInvariant();
        var accounts = await _dbContext.LedgerAccounts
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);

        return accounts
            .Select(account => account.Code.Value)
            .Where(code => code.StartsWith(normalizedPrefix, StringComparison.Ordinal))
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();
    }
}
