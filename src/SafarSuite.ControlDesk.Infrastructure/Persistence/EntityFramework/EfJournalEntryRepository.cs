using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfJournalEntryRepository : IJournalEntryRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfJournalEntryRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(JournalEntry journalEntry, CancellationToken cancellationToken = default)
    {
        await _dbContext.JournalEntries.AddAsync(journalEntry, cancellationToken);
    }

    public async Task<JournalEntry?> GetByIdAsync(JournalEntryId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.JournalEntries
            .Include(entry => entry.Lines)
            .SingleOrDefaultAsync(entry => entry.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<JournalEntry>> ListAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        JournalSourceType? sourceType = null,
        CancellationToken cancellationToken = default)
    {
        return await FilterEntries(fromDate, toDate, sourceType)
            .Include(entry => entry.Lines)
            .OrderBy(entry => entry.EntryDate)
            .ThenBy(entry => entry.CreatedAtUtc)
            .ThenBy(entry => entry.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<JournalEntry>> ListForLedgerAccountAsync(
        LedgerAccountId ledgerAccountId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        return await FilterEntries(fromDate, toDate, sourceType: null)
            .Include(entry => entry.Lines)
            .Where(entry => entry.Lines.Any(line => line.LedgerAccountId == ledgerAccountId))
            .OrderBy(entry => entry.EntryDate)
            .ThenBy(entry => entry.CreatedAtUtc)
            .ThenBy(entry => entry.Id)
            .ToArrayAsync(cancellationToken);
    }

    private IQueryable<JournalEntry> FilterEntries(
        DateOnly? fromDate,
        DateOnly? toDate,
        JournalSourceType? sourceType)
    {
        var entries = _dbContext.JournalEntries.AsQueryable();

        if (fromDate.HasValue)
        {
            entries = entries.Where(entry => entry.EntryDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            entries = entries.Where(entry => entry.EntryDate <= toDate.Value);
        }

        if (sourceType.HasValue)
        {
            entries = entries.Where(entry => entry.SourceType == sourceType.Value);
        }

        return entries;
    }
}
