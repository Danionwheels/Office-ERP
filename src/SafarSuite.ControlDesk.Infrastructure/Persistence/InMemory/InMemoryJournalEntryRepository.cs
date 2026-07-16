using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryJournalEntryRepository : IJournalEntryRepository
{
    private readonly ConcurrentDictionary<Guid, JournalEntry> _entriesById = new();

    public Task AddAsync(JournalEntry journalEntry, CancellationToken cancellationToken = default)
    {
        _entriesById.TryAdd(journalEntry.Id.Value, journalEntry);

        return Task.CompletedTask;
    }

    public Task<JournalEntry?> GetByIdAsync(JournalEntryId id, CancellationToken cancellationToken = default)
    {
        _entriesById.TryGetValue(id.Value, out var journalEntry);

        return Task.FromResult(journalEntry);
    }

    public Task<IReadOnlyCollection<JournalEntry>> ListForSourceDocumentAsync(
        JournalSourceType sourceType,
        Guid sourceDocumentId,
        CancellationToken cancellationToken = default)
    {
        var entries = _entriesById.Values
            .Where(entry => entry.SourceType == sourceType)
            .Where(entry => entry.SourceDocumentId == sourceDocumentId)
            .OrderBy(entry => entry.EntryDate)
            .ThenBy(entry => entry.CreatedAtUtc)
            .ThenBy(entry => entry.Id.Value)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<JournalEntry>>(entries);
    }

    public Task<int> GetMaximumVoucherSequenceAsync(
        JournalSourceType sourceType,
        string prefix,
        int sequenceYear,
        CancellationToken cancellationToken = default)
    {
        var maximum = _entriesById.Values
            .Where(entry => entry.SourceType == sourceType)
            .Select(entry => ParseVoucherSequence(entry.SourceReference, prefix, sequenceYear))
            .DefaultIfEmpty(0)
            .Max();

        return Task.FromResult(maximum);
    }

    public Task<IReadOnlyCollection<JournalEntry>> ListAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        JournalSourceType? sourceType = null,
        CancellationToken cancellationToken = default)
    {
        var entries = FilterEntries(fromDate, toDate, sourceType)
            .OrderBy(entry => entry.EntryDate)
            .ThenBy(entry => entry.CreatedAtUtc)
            .ThenBy(entry => entry.Id.Value)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<JournalEntry>>(entries);
    }

    public Task<IReadOnlyCollection<JournalEntry>> ListForLedgerAccountAsync(
        LedgerAccountId ledgerAccountId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var entries = FilterEntries(fromDate, toDate, sourceType: null)
            .Where(entry => entry.Lines.Any(line => line.LedgerAccountId == ledgerAccountId))
            .OrderBy(entry => entry.EntryDate)
            .ThenBy(entry => entry.CreatedAtUtc)
            .ThenBy(entry => entry.Id.Value)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<JournalEntry>>(entries);
    }

    private IEnumerable<JournalEntry> FilterEntries(
        DateOnly? fromDate,
        DateOnly? toDate,
        JournalSourceType? sourceType)
    {
        var entries = _entriesById.Values.AsEnumerable();

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

    private static int ParseVoucherSequence(
        string? sourceReference,
        string prefix,
        int sequenceYear)
    {
        var parts = sourceReference?.Split('-', StringSplitOptions.RemoveEmptyEntries);

        return parts is { Length: 3 }
            && string.Equals(parts[0], prefix, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(parts[1], out var year)
            && year == sequenceYear
            && int.TryParse(parts[2], out var sequence)
                ? sequence
                : 0;
    }
}
