using SafarSuite.ControlDesk.Application.Common.Paging;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ListJournalEntries;

public sealed class ListJournalEntriesHandler
{
    private const int MaximumPageSize = 100;
    private const int MaximumSearchLength = 128;

    private readonly IJournalEntryRegisterReader _journalEntries;

    public ListJournalEntriesHandler(IJournalEntryRegisterReader journalEntries)
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

        if (query.Take is < 1 or > MaximumPageSize)
        {
            return Result<ListJournalEntriesResult>.Failure(ApplicationError.Validation(
                nameof(query.Take),
                $"Page size must be between 1 and {MaximumPageSize}."));
        }

        var search = query.Search?.Trim().ToLowerInvariant() ?? string.Empty;

        if (search.Length > MaximumSearchLength)
        {
            return Result<ListJournalEntriesResult>.Failure(ApplicationError.Validation(
                nameof(query.Search),
                $"Search text must be {MaximumSearchLength} characters or fewer."));
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

        var normalizedSourceType = sourceType?.ToString();

        if (!OpaqueCursor.TryDecode<JournalRegisterCursor>(query.Cursor, out var cursor)
            || !CursorMatches(cursor, query, search, normalizedSourceType))
        {
            return Result<ListJournalEntriesResult>.Failure(ApplicationError.Validation(
                nameof(query.Cursor),
                "Journal cursor is invalid, malformed, or belongs to another query."));
        }

        var page = await _journalEntries.ReadPageAsync(
            new JournalEntryRegisterReadRequest(
                query.FromDate,
                query.ToDate,
                search,
                normalizedSourceType,
                cursor?.EntryDate,
                cursor?.CreatedAtUtc,
                cursor?.JournalEntryId,
                query.Take + 1),
            cancellationToken);
        var hasMore = page.Items.Count > query.Take;
        var entries = page.Items.Take(query.Take).ToArray();
        var nextCursor = hasMore && entries.Length > 0
            ? OpaqueCursor.Encode(new JournalRegisterCursor(
                1,
                query.FromDate,
                query.ToDate,
                search,
                normalizedSourceType,
                entries[^1].EntryDate,
                entries[^1].CreatedAtUtc,
                entries[^1].JournalEntryId))
            : null;

        return Result<ListJournalEntriesResult>.Success(new ListJournalEntriesResult(
            entries.Select(entry => new JournalEntryRegisterItemResult(
                entry.JournalEntryId,
                entry.EntryDate,
                entry.CurrencyCode,
                entry.SourceType,
                entry.SourceReference,
                entry.Memo,
                entry.Status,
                entry.TotalDebit,
                entry.TotalCredit,
                entry.LineCount)).ToArray(),
            query.Take,
            hasMore,
            nextCursor,
            page.FilteredCount));
    }

    private static bool CursorMatches(
        JournalRegisterCursor? cursor,
        ListJournalEntriesQuery query,
        string search,
        string? sourceType)
    {
        if (cursor is null)
        {
            return true;
        }

        return cursor.Version == 1
            && cursor.JournalEntryId != Guid.Empty
            && cursor.FromDate == query.FromDate
            && cursor.ToDate == query.ToDate
            && string.Equals(cursor.Search, search, StringComparison.Ordinal)
            && string.Equals(cursor.SourceType, sourceType, StringComparison.Ordinal);
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

    private sealed record JournalRegisterCursor(
        int Version,
        DateOnly? FromDate,
        DateOnly? ToDate,
        string Search,
        string? SourceType,
        DateOnly EntryDate,
        DateTimeOffset CreatedAtUtc,
        Guid JournalEntryId);
}
