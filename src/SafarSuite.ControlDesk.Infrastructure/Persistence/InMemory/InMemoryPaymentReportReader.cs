using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryPaymentReportReader : IPaymentReportReader
{
    private readonly InMemoryPaymentRepository _payments;
    private readonly InMemoryClientRepository _clients;
    private readonly InMemoryInvoiceRepository _invoices;
    private readonly IJournalEntryRepository _journalEntries;

    public InMemoryPaymentReportReader(
        InMemoryPaymentRepository payments,
        InMemoryClientRepository clients,
        InMemoryInvoiceRepository invoices,
        IJournalEntryRepository journalEntries)
    {
        _payments = payments;
        _clients = clients;
        _invoices = invoices;
        _journalEntries = journalEntries;
    }

    public async Task<PaymentReceiptReportReadPage> ReadReceiptsPageAsync(
        PaymentReceiptReportReadRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var clientsById = _clients.Snapshot()
            .ToDictionary(client => client.Id.Value);
        var invoicesById = _invoices.Snapshot()
            .ToDictionary(invoice => invoice.Id.Value);
        var journals = await _journalEntries.ListAsync(cancellationToken: cancellationToken);
        var filtered = _payments.Snapshot()
            .Where(payment => !request.ClientId.HasValue
                || payment.ClientId.Value == request.ClientId.Value)
            .Where(payment => !request.FromDate.HasValue
                || payment.ReceivedOn >= request.FromDate.Value)
            .Where(payment => !request.ToDate.HasValue
                || payment.ReceivedOn <= request.ToDate.Value)
            .Where(payment => request.Method is null
                || string.Equals(payment.Method.ToString(), request.Method, StringComparison.Ordinal))
            .Where(payment => request.Status is null
                || string.Equals(payment.Status.ToString(), request.Status, StringComparison.Ordinal))
            .Where(payment => request.CurrencyCode is null
                || string.Equals(
                    payment.Amount.CurrencyCode,
                    request.CurrencyCode,
                    StringComparison.Ordinal))
            .Where(payment => clientsById.ContainsKey(payment.ClientId.Value))
            .Where(payment => invoicesById.ContainsKey(payment.InvoiceId.Value))
            .Select(payment =>
            {
                var client = clientsById[payment.ClientId.Value];
                var invoice = invoicesById[payment.InvoiceId.Value];
                var journalEntryId = journals
                    .Where(entry => entry.ClientId?.Value == payment.ClientId.Value)
                    .Where(entry => entry.SourceDocumentId == payment.Id.Value)
                    .Where(entry => entry.SourceType is
                        JournalSourceType.PaymentReceipt or JournalSourceType.PaymentReversal)
                    .OrderByDescending(entry => entry.EntryDate)
                    .ThenByDescending(entry => entry.CreatedAtUtc)
                    .ThenByDescending(entry => entry.Id.Value)
                    .Select(entry => (Guid?)entry.Id.Value)
                    .FirstOrDefault();

                return new PaymentReceiptReportReadItem(
                    payment.Id.Value,
                    payment.ClientId.Value,
                    client.Code.Value,
                    client.DisplayName,
                    payment.InvoiceId.Value,
                    invoice.Number.Value,
                    payment.Reference.Value,
                    payment.Method.ToString(),
                    payment.Status.ToString(),
                    payment.Amount.Amount,
                    payment.Amount.CurrencyCode,
                    payment.ReceivedOn,
                    journalEntryId,
                    payment.RecordedAtUtc);
            })
            .ToArray();
        var filteredCount = filtered.LongLength;
        var page = filtered
            .Where(item => IsAfterCursor(item, request))
            .OrderByDescending(item => item.ReceivedOn)
            .ThenByDescending(item => item.RecordedAtUtc)
            .ThenByDescending(item => item.PaymentId)
            .Take(request.Take)
            .ToArray();

        return new PaymentReceiptReportReadPage(page, filteredCount);
    }

    private static bool IsAfterCursor(
        PaymentReceiptReportReadItem item,
        PaymentReceiptReportReadRequest request)
    {
        if (!request.AfterPaymentId.HasValue)
        {
            return true;
        }

        var comparison = item.ReceivedOn.CompareTo(request.AfterReceivedOn ?? DateOnly.MinValue);

        if (comparison == 0)
        {
            comparison = item.RecordedAtUtc.CompareTo(
                request.AfterRecordedAtUtc ?? DateTimeOffset.MinValue);
        }

        if (comparison == 0)
        {
            comparison = item.PaymentId.CompareTo(request.AfterPaymentId.Value);
        }

        return comparison < 0;
    }
}
