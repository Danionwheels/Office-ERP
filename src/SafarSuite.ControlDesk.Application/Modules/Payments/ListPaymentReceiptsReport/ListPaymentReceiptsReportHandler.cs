using SafarSuite.ControlDesk.Application.Common.Paging;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.ListPaymentReceiptsReport;

public sealed class ListPaymentReceiptsReportHandler
{
    private const int MaximumPageSize = 100;
    private readonly IPaymentReportReader _reportReader;

    public ListPaymentReceiptsReportHandler(IPaymentReportReader reportReader)
    {
        _reportReader = reportReader;
    }

    public async Task<Result<ListPaymentReceiptsReportResult>> HandleAsync(
        ListPaymentReceiptsReportQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.ClientId == Guid.Empty)
        {
            return Result<ListPaymentReceiptsReportResult>.Failure(ApplicationError.Validation(
                nameof(query.ClientId),
                "Client id must be a non-empty identifier when supplied."));
        }

        if (query.FromDate.HasValue
            && query.ToDate.HasValue
            && query.FromDate.Value > query.ToDate.Value)
        {
            return Result<ListPaymentReceiptsReportResult>.Failure(ApplicationError.Validation(
                nameof(query.FromDate),
                "From date cannot be after to date."));
        }

        if (query.Take is < 1 or > MaximumPageSize)
        {
            return Result<ListPaymentReceiptsReportResult>.Failure(ApplicationError.Validation(
                nameof(query.Take),
                $"Page size must be between 1 and {MaximumPageSize}."));
        }

        var methodFilter = NormalizeEnumFilter<PaymentMethod>(
            query.Method,
            nameof(query.Method),
            "Payment method");

        if (methodFilter.Error is not null)
        {
            return Result<ListPaymentReceiptsReportResult>.Failure(methodFilter.Error);
        }

        var statusFilter = NormalizeEnumFilter<PaymentStatus>(
            query.Status,
            nameof(query.Status),
            "Payment status");

        if (statusFilter.Error is not null)
        {
            return Result<ListPaymentReceiptsReportResult>.Failure(statusFilter.Error);
        }

        var currencyCode = string.IsNullOrWhiteSpace(query.CurrencyCode)
            || string.Equals(query.CurrencyCode.Trim(), "All", StringComparison.OrdinalIgnoreCase)
            ? null
            : query.CurrencyCode.Trim().ToUpperInvariant();

        if (currencyCode is not null && !CurrencyCodeValidation.IsValid(currencyCode))
        {
            return Result<ListPaymentReceiptsReportResult>.Failure(ApplicationError.Validation(
                nameof(query.CurrencyCode),
                "Currency code must be three ASCII letters when supplied."));
        }

        if (!OpaqueCursor.TryDecode<PaymentReceiptCursor>(query.Cursor, out var cursor)
            || !CursorMatches(cursor, query, methodFilter.Value, statusFilter.Value, currencyCode))
        {
            return Result<ListPaymentReceiptsReportResult>.Failure(ApplicationError.Validation(
                nameof(query.Cursor),
                "Receipt cursor is invalid, malformed, or belongs to another query."));
        }

        var page = await _reportReader.ReadReceiptsPageAsync(
            new PaymentReceiptReportReadRequest(
                query.ClientId,
                query.FromDate,
                query.ToDate,
                methodFilter.Value,
                statusFilter.Value,
                currencyCode,
                cursor?.ReceivedOn,
                cursor?.RecordedAtUtc,
                cursor?.PaymentId,
                query.Take + 1),
            cancellationToken);
        var hasMore = page.Items.Count > query.Take;
        var items = page.Items.Take(query.Take).ToArray();
        var nextCursor = hasMore && items.Length > 0
            ? OpaqueCursor.Encode(new PaymentReceiptCursor(
                1,
                query.ClientId,
                query.FromDate,
                query.ToDate,
                methodFilter.Value,
                statusFilter.Value,
                currencyCode,
                items[^1].ReceivedOn,
                items[^1].RecordedAtUtc,
                items[^1].PaymentId))
            : null;

        return Result<ListPaymentReceiptsReportResult>.Success(new ListPaymentReceiptsReportResult(
            items.Select(payment => new PaymentReceiptReportItemResult(
                payment.PaymentId,
                payment.ClientId,
                payment.ClientCode,
                payment.ClientName,
                payment.InvoiceId,
                payment.InvoiceNumber,
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

    private static (string? Value, ApplicationError? Error) NormalizeEnumFilter<TEnum>(
        string? value,
        string target,
        string label)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(value.Trim(), "All", StringComparison.OrdinalIgnoreCase))
        {
            return (null, null);
        }

        if (!Enum.TryParse<TEnum>(value.Trim(), ignoreCase: true, out var parsed)
            || !Enum.IsDefined(parsed))
        {
            return (null, ApplicationError.Validation(target, $"{label} is not valid."));
        }

        return (parsed.ToString(), null);
    }

    private static bool CursorMatches(
        PaymentReceiptCursor? cursor,
        ListPaymentReceiptsReportQuery query,
        string? method,
        string? status,
        string? currencyCode)
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
            && string.Equals(cursor.Method, method, StringComparison.Ordinal)
            && string.Equals(cursor.Status, status, StringComparison.Ordinal)
            && string.Equals(cursor.CurrencyCode, currencyCode, StringComparison.Ordinal);
    }

    private sealed record PaymentReceiptCursor(
        int Version,
        Guid? ClientId,
        DateOnly? FromDate,
        DateOnly? ToDate,
        string? Method,
        string? Status,
        string? CurrencyCode,
        DateOnly ReceivedOn,
        DateTimeOffset RecordedAtUtc,
        Guid PaymentId);
}
