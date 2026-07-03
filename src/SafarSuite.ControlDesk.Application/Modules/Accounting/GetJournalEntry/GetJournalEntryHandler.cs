using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListJournalEntries;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetJournalEntry;

public sealed class GetJournalEntryHandler
{
    private readonly IJournalEntryRepository _journalEntries;

    public GetJournalEntryHandler(IJournalEntryRepository journalEntries)
    {
        _journalEntries = journalEntries;
    }

    public async Task<Result<JournalEntrySummaryResult>> HandleAsync(
        GetJournalEntryQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.JournalEntryId == Guid.Empty)
        {
            return Result<JournalEntrySummaryResult>.Failure(ApplicationError.Validation(
                nameof(query.JournalEntryId),
                "Journal entry id cannot be empty."));
        }

        var entry = await _journalEntries.GetByIdAsync(
            JournalEntryId.Create(query.JournalEntryId),
            cancellationToken);

        return entry is null
            ? Result<JournalEntrySummaryResult>.Failure(ApplicationError.NotFound(
                nameof(query.JournalEntryId),
                "Journal entry was not found."))
            : Result<JournalEntrySummaryResult>.Success(ListJournalEntriesHandler.ToSummary(entry));
    }
}
