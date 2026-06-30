using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryLedgerAccountRepository : ILedgerAccountRepository
{
    private readonly ConcurrentDictionary<Guid, LedgerAccount> _accountsById = new();

    public Task AddAsync(LedgerAccount ledgerAccount, CancellationToken cancellationToken = default)
    {
        _accountsById.TryAdd(ledgerAccount.Id.Value, ledgerAccount);

        return Task.CompletedTask;
    }

    public Task<LedgerAccount?> GetByIdAsync(LedgerAccountId id, CancellationToken cancellationToken = default)
    {
        _accountsById.TryGetValue(id.Value, out var ledgerAccount);

        return Task.FromResult(ledgerAccount);
    }

    public Task<bool> ExistsByCodeAsync(LedgerAccountCode code, CancellationToken cancellationToken = default)
    {
        var exists = _accountsById.Values.Any(account => account.Code.Equals(code));

        return Task.FromResult(exists);
    }
}
