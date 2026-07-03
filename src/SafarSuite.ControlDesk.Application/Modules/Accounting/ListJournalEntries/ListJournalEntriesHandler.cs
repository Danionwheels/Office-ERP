using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ListJournalEntries;

public sealed class ListJournalEntriesHandler
{
    private readonly IJournalEntryRepository _journalEntries;

    public ListJournalEntriesHandler(IJournalEntryRepository journalEntries)
    {
        _journalEntries = journalEntries;
    }

    public async Task<Result<ListJournalEntriesResult>> HandleAsync(
        ListJournalEntriesQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate.Value > query.ToDate.Value)
        {
            return Result<ListJournalEntriesResult>.Failure(ApplicationError.Validation(
                nameof(query.FromDate),
                "From date cannot be after to date."));
        }

        JournalSourceType? sourceType = null;

        if (!string.IsNullOrWhiteSpace(query.SourceType))
        {
            if (!Enum.TryParse<JournalSourceType>(query.SourceType, ignoreCase: true, out var parsedSourceType)
                || !Enum.IsDefined(parsedSourceType))
            {
                return Result<ListJournalEntriesResult>.Failure(ApplicationError.Validation(
                    nameof(query.SourceType),
                    "Journal source type is not valid."));
            }

            sourceType = parsedSourceType;
        }

        var entries = await _journalEntries.ListAsync(
            query.FromDate,
            query.ToDate,
            sourceType,
            cancellationToken);

        return Result<ListJournalEntriesResult>.Success(new ListJournalEntriesResult(
            entries.Select(ToSummary).ToArray()));
    }

    public static JournalEntrySummaryResult ToSummary(JournalEntry entry)
    {
        return new JournalEntrySummaryResult(
            entry.Id.Value,
            entry.EntryDate,
            entry.CurrencyCode,
            entry.SourceType.ToString(),
            entry.SourceReference,
            entry.Memo,
            entry.Status.ToString(),
            entry.TotalDebit.Amount,
            entry.TotalCredit.Amount,
            entry.Lines.Select(line => new JournalEntryLineResult(
                line.LedgerAccountId.Value,
                line.Debit.Amount,
                line.Credit.Amount,
                line.Description)).ToArray());
    }
}
