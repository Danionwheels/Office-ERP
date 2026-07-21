using SafarSuite.ControlDesk.Application.Common.Paging;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.Financials;

public sealed record ListClientPaymentsQuery(
    Guid ClientId,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    string? Search = null,
    string? Status = null,
    int Take = 25,
    string? Cursor = null);

public sealed class ListClientPaymentsHandler
{
    private readonly IClientFinancialReader _financials;

    public ListClientPaymentsHandler(IClientFinancialReader financials)
    {
        _financials = financials;
    }

    public async Task<Result<ClientPaymentPageResult>> HandleAsync(
        ListClientPaymentsQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationError = ClientFinancialQueryRules.ValidateClientAndDates(
            query.ClientId,
            query.FromDate,
            query.ToDate);

        if (validationError is not null)
        {
            return Result<ClientPaymentPageResult>.Failure(validationError);
        }

        var search = ClientFinancialQueryRules.NormalizeSearch(query.Search);
        validationError = ClientFinancialQueryRules.ValidatePage(query.Take, search);

        if (validationError is not null)
        {
            return Result<ClientPaymentPageResult>.Failure(validationError);
        }

        string? status = null;

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<PaymentStatus>(query.Status, ignoreCase: true, out var parsedStatus)
                || !Enum.IsDefined(parsedStatus))
            {
                return Result<ClientPaymentPageResult>.Failure(ApplicationError.Validation(
                    nameof(query.Status),
                    "Payment status is not valid."));
            }

            status = parsedStatus.ToString();
        }

        if (!OpaqueCursor.TryDecode<PaymentCursor>(query.Cursor, out var cursor)
            || !CursorMatches(cursor, query, search, status))
        {
            return Result<ClientPaymentPageResult>.Failure(ApplicationError.Validation(
                nameof(query.Cursor),
                "Payment cursor is invalid, malformed, or belongs to another query."));
        }

        var clientId = ClientId.Create(query.ClientId);

        if (!await _financials.ClientExistsAsync(clientId, cancellationToken))
        {
            return Result<ClientPaymentPageResult>.Failure(ApplicationError.NotFound(
                nameof(query.ClientId),
                "Client was not found."));
        }

        var page = await _financials.ReadPaymentPageAsync(
            new ClientPaymentReadRequest(
                clientId,
                query.FromDate,
                query.ToDate,
                search,
                status,
                cursor?.ReceivedOn,
                cursor?.RecordedAtUtc,
                cursor?.PaymentId,
                query.Take + 1),
            cancellationToken);
        var hasMore = page.Items.Count > query.Take;
        var items = page.Items.Take(query.Take).ToArray();
        var nextCursor = hasMore && items.Length > 0
            ? OpaqueCursor.Encode(new PaymentCursor(
                1,
                query.ClientId,
                query.FromDate,
                query.ToDate,
                search,
                status,
                items[^1].ReceivedOn,
                items[^1].RecordedAtUtc,
                items[^1].PaymentId))
            : null;

        return Result<ClientPaymentPageResult>.Success(new ClientPaymentPageResult(
            items.Select(payment => new ClientPaymentRegisterItemResult(
                payment.PaymentId,
                payment.InvoiceId,
                payment.Reference,
                payment.Method,
                payment.Status,
                payment.Amount,
                payment.CurrencyCode,
                payment.ReceivedOn,
                payment.JournalEntryId)).ToArray(),
            query.Take,
            hasMore,
            nextCursor,
            page.FilteredCount));
    }

    private static bool CursorMatches(
        PaymentCursor? cursor,
        ListClientPaymentsQuery query,
        string search,
        string? status)
    {
        if (cursor is null)
        {
            return true;
        }

        return cursor.Version == 1
            && cursor.ClientId == query.ClientId
            && cursor.PaymentId != Guid.Empty
            && cursor.FromDate == query.FromDate
            && cursor.ToDate == query.ToDate
            && string.Equals(cursor.Search, search, StringComparison.Ordinal)
            && string.Equals(cursor.Status, status, StringComparison.Ordinal);
    }

    private sealed record PaymentCursor(
        int Version,
        Guid ClientId,
        DateOnly? FromDate,
        DateOnly? ToDate,
        string Search,
        string? Status,
        DateOnly ReceivedOn,
        DateTimeOffset RecordedAtUtc,
        Guid PaymentId);
}
