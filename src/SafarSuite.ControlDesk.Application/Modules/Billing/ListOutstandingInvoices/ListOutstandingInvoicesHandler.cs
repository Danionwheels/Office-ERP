using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Paging;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.ListOutstandingInvoices;

public sealed class ListOutstandingInvoicesHandler
{
    private const int MaximumPageSize = 100;
    private readonly IBillingReportReader _reports;
    private readonly IClock _clock;

    public ListOutstandingInvoicesHandler(IBillingReportReader reports, IClock clock)
    {
        _reports = reports;
        _clock = clock;
    }

    public async Task<Result<ListOutstandingInvoicesResult>> HandleAsync(
        ListOutstandingInvoicesQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationError = Validate(query);
        if (validationError is not null)
        {
            return Result<ListOutstandingInvoicesResult>.Failure(validationError);
        }

        if (!TryNormalizeStatus(query.Status, out var status))
        {
            return Result<ListOutstandingInvoicesResult>.Failure(ApplicationError.Validation(
                nameof(query.Status),
                "Outstanding invoice status must be All, Issued, PartiallyPaid, or Overdue."));
        }

        var currencyCode = NormalizeCurrencyCode(query.CurrencyCode);
        if (currencyCode is not null && !CurrencyCodeValidation.IsValid(currencyCode))
        {
            return Result<ListOutstandingInvoicesResult>.Failure(ApplicationError.Validation(
                nameof(query.CurrencyCode),
                "Outstanding invoice currency code must be three ASCII letters."));
        }

        if (!OpaqueCursor.TryDecode<OutstandingInvoiceCursor>(query.Cursor, out var cursor)
            || !CursorMatches(cursor, query, status, currencyCode))
        {
            return Result<ListOutstandingInvoicesResult>.Failure(ApplicationError.Validation(
                nameof(query.Cursor),
                "Outstanding invoice cursor is invalid, malformed, or belongs to another query."));
        }

        var reportDate = cursor?.Today ?? _clock.Today;

        var page = await _reports.ReadOutstandingInvoicePageAsync(
            new OutstandingInvoiceReadRequest(
                query.ClientId,
                query.FromDate,
                query.ToDate,
                query.MinAmount,
                query.MaxAmount,
                status,
                currencyCode,
                reportDate,
                cursor?.IssueDate,
                cursor?.CreatedAtUtc,
                cursor?.InvoiceId,
                query.Take + 1),
            cancellationToken);
        var hasMore = page.Items.Count > query.Take;
        var items = page.Items.Take(query.Take).ToArray();
        var nextCursor = hasMore && items.Length > 0
            ? OpaqueCursor.Encode(new OutstandingInvoiceCursor(
                1,
                query.ClientId,
                query.FromDate,
                query.ToDate,
                query.MinAmount,
                query.MaxAmount,
                status,
                currencyCode,
                reportDate,
                items[^1].IssueDate,
                items[^1].CreatedAtUtc,
                items[^1].InvoiceId))
            : null;

        return Result<ListOutstandingInvoicesResult>.Success(new ListOutstandingInvoicesResult(
            items.Select(item => new OutstandingInvoiceResult(
                item.InvoiceId,
                item.ClientId,
                item.ClientCode,
                item.ClientName,
                item.InvoiceNumber,
                item.IssueDate,
                item.DueDate,
                item.Status,
                item.TotalAmount,
                item.AmountPaid,
                item.BalanceDue,
                item.CurrencyCode,
                item.DaysOverdue,
                item.AgingBucket,
                item.JournalEntryId)).ToArray(),
            query.Take,
            hasMore,
            nextCursor,
            page.FilteredCount));
    }

    private static ApplicationError? Validate(ListOutstandingInvoicesQuery query)
    {
        if (query.ClientId == Guid.Empty)
        {
            return ApplicationError.Validation(nameof(query.ClientId), "Client id cannot be empty.");
        }
        if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate.Value > query.ToDate.Value)
        {
            return ApplicationError.Validation(nameof(query.FromDate), "From date cannot be after to date.");
        }
        if (query.MinAmount < 0)
        {
            return ApplicationError.Validation(nameof(query.MinAmount), "Minimum amount cannot be negative.");
        }
        if (query.MaxAmount < 0)
        {
            return ApplicationError.Validation(nameof(query.MaxAmount), "Maximum amount cannot be negative.");
        }
        if (query.MinAmount.HasValue && query.MaxAmount.HasValue && query.MinAmount.Value > query.MaxAmount.Value)
        {
            return ApplicationError.Validation(nameof(query.MinAmount), "Minimum amount cannot exceed maximum amount.");
        }
        return query.Take is < 1 or > MaximumPageSize
            ? ApplicationError.Validation(nameof(query.Take), $"Page size must be between 1 and {MaximumPageSize}.")
            : null;
    }

    private static bool TryNormalizeStatus(string? value, out string status)
    {
        status = string.IsNullOrWhiteSpace(value) ? "All" : value.Trim();
        if (!Enum.TryParse<OutstandingInvoiceStatusFilter>(status, ignoreCase: true, out var parsed)
            || !Enum.IsDefined(parsed))
        {
            return false;
        }
        status = parsed.ToString();
        return true;
    }

    private static string? NormalizeCurrencyCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            || string.Equals(value.Trim(), "All", StringComparison.OrdinalIgnoreCase)
                ? null
                : value.Trim().ToUpperInvariant();
    }

    private static bool CursorMatches(
        OutstandingInvoiceCursor? cursor,
        ListOutstandingInvoicesQuery query,
        string status,
        string? currencyCode)
    {
        return cursor is null
            || (cursor.Version == 1
                && cursor.InvoiceId != Guid.Empty
                && cursor.ClientId == query.ClientId
                && cursor.FromDate == query.FromDate
                && cursor.ToDate == query.ToDate
                && cursor.MinAmount == query.MinAmount
                && cursor.MaxAmount == query.MaxAmount
                && string.Equals(cursor.Status, status, StringComparison.Ordinal)
                && string.Equals(cursor.CurrencyCode, currencyCode, StringComparison.Ordinal));
    }

    private enum OutstandingInvoiceStatusFilter
    {
        All = 1,
        Issued = 2,
        PartiallyPaid = 3,
        Overdue = 4
    }

    private sealed record OutstandingInvoiceCursor(
        int Version,
        Guid? ClientId,
        DateOnly? FromDate,
        DateOnly? ToDate,
        decimal? MinAmount,
        decimal? MaxAmount,
        string Status,
        string? CurrencyCode,
        DateOnly Today,
        DateOnly IssueDate,
        DateTimeOffset CreatedAtUtc,
        Guid InvoiceId);
}
