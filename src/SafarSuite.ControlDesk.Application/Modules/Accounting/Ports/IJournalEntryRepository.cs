using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;

public interface IJournalEntryRepository
{
    Task AddAsync(JournalEntry journalEntry, CancellationToken cancellationToken = default);

    Task<JournalEntry?> GetByIdAsync(JournalEntryId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<JournalEntry>> ListAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        JournalSourceType? sourceType = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<JournalEntry>> ListForLedgerAccountAsync(
        LedgerAccountId ledgerAccountId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);
}
