using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;

public interface ILedgerAccountRepository
{
    Task AddAsync(LedgerAccount ledgerAccount, CancellationToken cancellationToken = default);

    Task<LedgerAccount?> GetByIdAsync(LedgerAccountId id, CancellationToken cancellationToken = default);

    Task<LedgerAccount?> GetByCodeAsync(LedgerAccountCode code, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeAsync(LedgerAccountCode code, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<LedgerAccount>> ListAsync(
        string? search = null,
        LedgerAccountType? type = null,
        LedgerAccountStatus? status = null,
        bool? isPostingAccount = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> ListCodesByPrefixAsync(
        string codePrefix,
        CancellationToken cancellationToken = default);
}
