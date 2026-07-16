using SafarSuite.ControlDesk.Application.Common.Paging;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.Financials;

public sealed record ListClientInvoicesQuery(
    Guid ClientId,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    string? Search = null,
    string? State = null,
    int Take = 25,
    string? Cursor = null);

public sealed class ListClientInvoicesHandler
{
    private readonly IClientFinancialReader _financials;

    public ListClientInvoicesHandler(IClientFinancialReader financials)
    {
        _financials = financials;
    }

    public async Task<Result<ClientInvoicePageResult>> HandleAsync(
        ListClientInvoicesQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationError = ClientFinancialQueryRules.ValidateClientAndDates(
            query.ClientId,
            query.FromDate,
            query.ToDate);

        if (validationError is not null)
        {
            return Result<ClientInvoicePageResult>.Failure(validationError);
        }

        var search = ClientFinancialQueryRules.NormalizeSearch(query.Search);
        validationError = ClientFinancialQueryRules.ValidatePage(query.Take, search);

        if (validationError is not null)
        {
            return Result<ClientInvoicePageResult>.Failure(validationError);
        }

        if (!TryParseState(query.State, out var state))
        {
            return Result<ClientInvoicePageResult>.Failure(ApplicationError.Validation(
                nameof(query.State),
                "Invoice state must be all, open, paid, draft, or void."));
        }

        if (!OpaqueCursor.TryDecode<InvoiceCursor>(query.Cursor, out var cursor)
            || !CursorMatches(cursor, query, search, state))
        {
            return Result<ClientInvoicePageResult>.Failure(ApplicationError.Validation(
                nameof(query.Cursor),
                "Invoice cursor is invalid, malformed, or belongs to another query."));
        }

        var clientId = ClientId.Create(query.ClientId);

        if (!await _financials.ClientExistsAsync(clientId, cancellationToken))
        {
            return Result<ClientInvoicePageResult>.Failure(ApplicationError.NotFound(
                nameof(query.ClientId),
                "Client was not found."));
        }

        var page = await _financials.ReadInvoicePageAsync(
            new ClientInvoiceReadRequest(
                clientId,
                query.FromDate,
                query.ToDate,
                search,
                state,
                cursor?.IssueDate,
                cursor?.CreatedAtUtc,
                cursor?.InvoiceId,
                query.Take + 1),
            cancellationToken);
        var hasMore = page.Items.Count > query.Take;
        var items = page.Items.Take(query.Take).ToArray();
        var nextCursor = hasMore && items.Length > 0
            ? OpaqueCursor.Encode(new InvoiceCursor(
                1,
                query.ClientId,
                query.FromDate,
                query.ToDate,
                search,
                state.ToString(),
                items[^1].IssueDate,
                items[^1].CreatedAtUtc,
                items[^1].InvoiceId))
            : null;

        return Result<ClientInvoicePageResult>.Success(new ClientInvoicePageResult(
            items.Select(invoice => new ClientInvoiceRegisterItemResult(
                invoice.InvoiceId,
                invoice.ContractId,
                invoice.InvoiceNumber,
                invoice.IssueDate,
                invoice.DueDate,
                invoice.Status,
                invoice.TotalAmount,
                invoice.AmountPaid,
                invoice.BalanceDue,
                invoice.CurrencyCode,
                invoice.JournalEntryId)).ToArray(),
            query.Take,
            hasMore,
            nextCursor,
            page.FilteredCount));
    }

    private static bool TryParseState(string? value, out ClientInvoiceRegisterState state)
    {
        state = ClientInvoiceRegisterState.All;

        return string.IsNullOrWhiteSpace(value)
            || Enum.TryParse(value, ignoreCase: true, out state) && Enum.IsDefined(state);
    }

    private static bool CursorMatches(
        InvoiceCursor? cursor,
        ListClientInvoicesQuery query,
        string search,
        ClientInvoiceRegisterState state)
    {
        if (cursor is null)
        {
            return true;
        }

        return cursor.Version == 1
            && cursor.ClientId == query.ClientId
            && cursor.InvoiceId != Guid.Empty
            && cursor.FromDate == query.FromDate
            && cursor.ToDate == query.ToDate
            && string.Equals(cursor.Search, search, StringComparison.Ordinal)
            && string.Equals(cursor.State, state.ToString(), StringComparison.Ordinal);
    }

    private sealed record InvoiceCursor(
        int Version,
        Guid ClientId,
        DateOnly? FromDate,
        DateOnly? ToDate,
        string Search,
        string State,
        DateOnly IssueDate,
        DateTimeOffset CreatedAtUtc,
        Guid InvoiceId);
}
