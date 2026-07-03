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

    public Task<LedgerAccount?> GetByCodeAsync(
        LedgerAccountCode code,
        CancellationToken cancellationToken = default)
    {
        var ledgerAccount = _accountsById.Values.SingleOrDefault(account => account.Code.Equals(code));

        return Task.FromResult(ledgerAccount);
    }

    public Task<bool> ExistsByCodeAsync(LedgerAccountCode code, CancellationToken cancellationToken = default)
    {
        var exists = _accountsById.Values.Any(account => account.Code.Equals(code));

        return Task.FromResult(exists);
    }

    public Task<IReadOnlyCollection<LedgerAccount>> ListAsync(
        string? search = null,
        LedgerAccountType? type = null,
        LedgerAccountStatus? status = null,
        bool? isPostingAccount = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = search?.Trim();
        IEnumerable<LedgerAccount> accounts = _accountsById.Values;

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            accounts = accounts.Where(account =>
                account.Code.Value.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
                || account.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase));
        }

        if (type.HasValue)
        {
            accounts = accounts.Where(account => account.Type == type.Value);
        }

        if (status.HasValue)
        {
            accounts = accounts.Where(account => account.Status == status.Value);
        }

        if (isPostingAccount.HasValue)
        {
            accounts = accounts.Where(account => account.IsPostingAccount == isPostingAccount.Value);
        }

        IReadOnlyCollection<LedgerAccount> result = accounts
            .OrderBy(account => account.Code.Value, StringComparer.Ordinal)
            .ThenBy(account => account.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult(result);
    }

    public Task<IReadOnlyCollection<string>> ListCodesByPrefixAsync(
        string codePrefix,
        CancellationToken cancellationToken = default)
    {
        var normalizedPrefix = codePrefix.Trim().ToUpperInvariant();
        IReadOnlyCollection<string> codes = _accountsById.Values
            .Select(account => account.Code.Value)
            .Where(code => code.StartsWith(normalizedPrefix, StringComparison.Ordinal))
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();

        return Task.FromResult(codes);
    }
}
