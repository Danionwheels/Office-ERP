using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryJournalEntryRegisterReader : IJournalEntryRegisterReader
{
    private readonly IJournalEntryRepository _journalEntries;

    public InMemoryJournalEntryRegisterReader(IJournalEntryRepository journalEntries)
    {
        _journalEntries = journalEntries;
    }

    public async Task<JournalEntryRegisterReadPage> ReadPageAsync(
        JournalEntryRegisterReadRequest request,
        CancellationToken cancellationToken = default)
    {
        JournalSourceType? sourceType = request.SourceType is null
            ? null
            : Enum.Parse<JournalSourceType>(request.SourceType);
        var entries = await _journalEntries.ListAsync(
            request.FromDate,
            request.ToDate,
            sourceType,
            cancellationToken);
        var filtered = entries
            .Where(entry => request.Search.Length == 0
                || (entry.SourceReference?.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (entry.Memo?.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ?? false)
                || entry.SourceType.ToString().Contains(request.Search, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(entry => entry.EntryDate)
            .ThenByDescending(entry => entry.CreatedAtUtc)
            .ThenByDescending(entry => entry.Id.Value)
            .ToArray();
        var page = filtered
            .Where(entry => IsAfterCursor(entry, request))
            .Take(request.Take)
            .Select(entry => new JournalEntryRegisterReadItem(
                entry.Id.Value,
                entry.EntryDate,
                entry.CurrencyCode,
                entry.SourceType.ToString(),
                entry.SourceReference,
                entry.Memo,
                entry.Status.ToString(),
                entry.TotalDebit.Amount,
                entry.TotalCredit.Amount,
                entry.Lines.Count,
                entry.CreatedAtUtc))
            .ToArray();

        return new JournalEntryRegisterReadPage(page, filtered.LongLength);
    }

    private static bool IsAfterCursor(
        JournalEntry entry,
        JournalEntryRegisterReadRequest request)
    {
        if (!request.AfterJournalEntryId.HasValue)
        {
            return true;
        }

        return entry.EntryDate < request.AfterEntryDate
            || entry.EntryDate == request.AfterEntryDate
            && (entry.CreatedAtUtc < request.AfterCreatedAtUtc
                || entry.CreatedAtUtc == request.AfterCreatedAtUtc
                && entry.Id.Value.CompareTo(request.AfterJournalEntryId.Value) < 0);
    }
}
