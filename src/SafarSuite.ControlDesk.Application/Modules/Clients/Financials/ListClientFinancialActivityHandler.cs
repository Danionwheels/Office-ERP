using SafarSuite.ControlDesk.Application.Common.Paging;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.Financials;

public sealed record ListClientFinancialActivityQuery(
    Guid ClientId,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    string? Search = null,
    int Take = 25,
    string? Cursor = null);

public sealed class ListClientFinancialActivityHandler
{
    private readonly IClientFinancialReader _financials;

    public ListClientFinancialActivityHandler(IClientFinancialReader financials)
    {
        _financials = financials;
    }

    public async Task<Result<ClientFinancialActivityPageResult>> HandleAsync(
        ListClientFinancialActivityQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationError = ClientFinancialQueryRules.ValidateClientAndDates(
            query.ClientId,
            query.FromDate,
            query.ToDate);

        if (validationError is not null)
        {
            return Result<ClientFinancialActivityPageResult>.Failure(validationError);
        }

        var search = ClientFinancialQueryRules.NormalizeSearch(query.Search);
        validationError = ClientFinancialQueryRules.ValidatePage(query.Take, search);

        if (validationError is not null)
        {
            return Result<ClientFinancialActivityPageResult>.Failure(validationError);
        }

        if (!OpaqueCursor.TryDecode<ActivityCursor>(query.Cursor, out var cursor)
            || !CursorMatches(cursor, query, search))
        {
            return Result<ClientFinancialActivityPageResult>.Failure(ApplicationError.Validation(
                nameof(query.Cursor),
                "Financial activity cursor is invalid, malformed, or belongs to another query."));
        }

        var clientId = ClientId.Create(query.ClientId);

        if (!await _financials.ClientExistsAsync(clientId, cancellationToken))
        {
            return Result<ClientFinancialActivityPageResult>.Failure(ApplicationError.NotFound(
                nameof(query.ClientId),
                "Client was not found."));
        }

        var page = await _financials.ReadActivityPageAsync(
            new ClientFinancialActivityReadRequest(
                clientId,
                query.FromDate,
                query.ToDate,
                search,
                cursor?.EntryDate,
                cursor?.SortOrder,
                cursor?.Reference,
                cursor?.DocumentId,
                query.Take + 1),
            cancellationToken);
        var hasMore = page.Items.Count > query.Take;
        var items = page.Items.Take(query.Take).ToArray();
        var nextCursor = hasMore && items.Length > 0
            ? OpaqueCursor.Encode(new ActivityCursor(
                1,
                query.ClientId,
                query.FromDate,
                query.ToDate,
                search,
                items[^1].EntryDate,
                items[^1].SortOrder,
                items[^1].Reference,
                items[^1].DocumentId))
            : null;

        return Result<ClientFinancialActivityPageResult>.Success(new ClientFinancialActivityPageResult(
            items.Select(line => new ClientFinancialActivityItemResult(
                line.EntryDate,
                line.DocumentType,
                line.Reference,
                line.InvoiceId,
                line.PaymentId,
                line.RefundId,
                line.CreditApplicationId,
                line.Description,
                line.Debit,
                line.Credit,
                line.RunningBalance,
                line.CurrencyCode,
                line.JournalEntryId)).ToArray(),
            query.Take,
            hasMore,
            nextCursor,
            page.FilteredCount));
    }

    private static bool CursorMatches(
        ActivityCursor? cursor,
        ListClientFinancialActivityQuery query,
        string search)
    {
        if (cursor is null)
        {
            return true;
        }

        return cursor.Version == 1
            && cursor.ClientId == query.ClientId
            && cursor.DocumentId != Guid.Empty
            && !string.IsNullOrWhiteSpace(cursor.Reference)
            && cursor.FromDate == query.FromDate
            && cursor.ToDate == query.ToDate
            && string.Equals(cursor.Search, search, StringComparison.Ordinal);
    }

    private sealed record ActivityCursor(
        int Version,
        Guid ClientId,
        DateOnly? FromDate,
        DateOnly? ToDate,
        string Search,
        DateOnly EntryDate,
        int SortOrder,
        string Reference,
        Guid DocumentId);
}
