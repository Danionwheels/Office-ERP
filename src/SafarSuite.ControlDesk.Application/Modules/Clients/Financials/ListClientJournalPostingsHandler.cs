using SafarSuite.ControlDesk.Application.Common.Paging;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.Financials;

public sealed record ListClientJournalPostingsQuery(
    Guid ClientId,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    string? Search = null,
    string? SourceType = null,
    int Take = 20,
    string? Cursor = null);

public sealed class ListClientJournalPostingsHandler
{
    private readonly IClientFinancialReader _financials;

    public ListClientJournalPostingsHandler(IClientFinancialReader financials)
    {
        _financials = financials;
    }

    public async Task<Result<ClientJournalPostingPageResult>> HandleAsync(
        ListClientJournalPostingsQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationError = ClientFinancialQueryRules.ValidateClientAndDates(
            query.ClientId,
            query.FromDate,
            query.ToDate);

        if (validationError is not null)
        {
            return Result<ClientJournalPostingPageResult>.Failure(validationError);
        }

        var search = ClientFinancialQueryRules.NormalizeSearch(query.Search);
        validationError = ClientFinancialQueryRules.ValidatePage(query.Take, search);

        if (validationError is not null)
        {
            return Result<ClientJournalPostingPageResult>.Failure(validationError);
        }

        string? sourceType = null;

        if (!string.IsNullOrWhiteSpace(query.SourceType))
        {
            if (!Enum.TryParse<JournalSourceType>(query.SourceType, ignoreCase: true, out var parsedSourceType)
                || !Enum.IsDefined(parsedSourceType))
            {
                return Result<ClientJournalPostingPageResult>.Failure(ApplicationError.Validation(
                    nameof(query.SourceType),
                    "Journal source type is not valid."));
            }

            sourceType = parsedSourceType.ToString();
        }

        if (!OpaqueCursor.TryDecode<JournalCursor>(query.Cursor, out var cursor)
            || !CursorMatches(cursor, query, search, sourceType))
        {
            return Result<ClientJournalPostingPageResult>.Failure(ApplicationError.Validation(
                nameof(query.Cursor),
                "Journal cursor is invalid, malformed, or belongs to another query."));
        }

        var clientId = ClientId.Create(query.ClientId);

        if (!await _financials.ClientExistsAsync(clientId, cancellationToken))
        {
            return Result<ClientJournalPostingPageResult>.Failure(ApplicationError.NotFound(
                nameof(query.ClientId),
                "Client was not found."));
        }

        var page = await _financials.ReadJournalPageAsync(
            new ClientJournalPostingReadRequest(
                clientId,
                query.FromDate,
                query.ToDate,
                search,
                sourceType,
                cursor?.EntryDate,
                cursor?.CreatedAtUtc,
                cursor?.JournalEntryId,
                query.Take + 1),
            cancellationToken);
        var hasMore = page.Items.Count > query.Take;
        var items = page.Items.Take(query.Take).ToArray();
        var nextCursor = hasMore && items.Length > 0
            ? OpaqueCursor.Encode(new JournalCursor(
                1,
                query.ClientId,
                query.FromDate,
                query.ToDate,
                search,
                sourceType,
                items[^1].EntryDate,
                items[^1].CreatedAtUtc,
                items[^1].JournalEntryId))
            : null;

        return Result<ClientJournalPostingPageResult>.Success(new ClientJournalPostingPageResult(
            items.Select(posting => new ClientJournalPostingItemResult(
                posting.JournalEntryId,
                posting.EntryDate,
                posting.SourceType,
                posting.SourceReference,
                posting.Memo,
                posting.Status,
                posting.TotalDebit,
                posting.TotalCredit,
                posting.CurrencyCode,
                posting.LineCount)).ToArray(),
            query.Take,
            hasMore,
            nextCursor,
            page.FilteredCount));
    }

    private static bool CursorMatches(
        JournalCursor? cursor,
        ListClientJournalPostingsQuery query,
        string search,
        string? sourceType)
    {
        if (cursor is null)
        {
            return true;
        }

        return cursor.Version == 1
            && cursor.ClientId == query.ClientId
            && cursor.JournalEntryId != Guid.Empty
            && cursor.FromDate == query.FromDate
            && cursor.ToDate == query.ToDate
            && string.Equals(cursor.Search, search, StringComparison.Ordinal)
            && string.Equals(cursor.SourceType, sourceType, StringComparison.Ordinal);
    }

    private sealed record JournalCursor(
        int Version,
        Guid ClientId,
        DateOnly? FromDate,
        DateOnly? ToDate,
        string Search,
        string? SourceType,
        DateOnly EntryDate,
        DateTimeOffset CreatedAtUtc,
        Guid JournalEntryId);
}
