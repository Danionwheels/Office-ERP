using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Billing;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryBillingReportReader : IBillingReportReader
{
    private readonly InMemoryClientRepository _clients;
    private readonly InMemoryInvoiceRepository _invoices;
    private readonly IJournalEntryRepository _journalEntries;

    public InMemoryBillingReportReader(
        InMemoryClientRepository clients,
        InMemoryInvoiceRepository invoices,
        IJournalEntryRepository journalEntries)
    {
        _clients = clients;
        _invoices = invoices;
        _journalEntries = journalEntries;
    }

    public Task<IReadOnlyCollection<AccountsReceivableAgingClientReadModel>> ReadAccountsReceivableAgingAsync(
        AccountsReceivableAgingReadRequest request,
        CancellationToken cancellationToken = default)
    {
        var clientsById = _clients.Snapshot().ToDictionary(client => client.Id);
        var rows = _invoices.Snapshot()
            .Where(IsOpenInvoice)
            .Where(invoice => invoice.IssueDate <= request.AsOfDate)
            .Where(invoice => string.Equals(
                invoice.CurrencyCode,
                request.CurrencyCode,
                StringComparison.Ordinal))
            .Select(invoice => new InvoiceResidual(invoice, invoice.BalanceDue.Amount))
            .Where(row => row.BalanceDue > 0)
            .Where(row => clientsById.ContainsKey(row.Invoice.ClientId))
            .GroupBy(row => row.Invoice.ClientId)
            .Select(group =>
            {
                var client = clientsById[group.Key];
                return new AccountsReceivableAgingClientReadModel(
                    client.Id.Value,
                    client.Code.Value,
                    client.DisplayName,
                    request.CurrencyCode,
                    SumBucket(group, request.AsOfDate, minimumDays: null, maximumDays: 0),
                    SumBucket(group, request.AsOfDate, 1, 30),
                    SumBucket(group, request.AsOfDate, 31, 60),
                    SumBucket(group, request.AsOfDate, 61, 90),
                    SumBucket(group, request.AsOfDate, 91, maximumDays: null),
                    group.Sum(row => row.BalanceDue),
                    group.LongCount());
            })
            .OrderBy(row => row.ClientName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.ClientCode, StringComparer.Ordinal)
            .ThenBy(row => row.ClientId)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<AccountsReceivableAgingClientReadModel>>(rows);
    }

    public async Task<OutstandingInvoiceReadPage> ReadOutstandingInvoicePageAsync(
        OutstandingInvoiceReadRequest request,
        CancellationToken cancellationToken = default)
    {
        var clientsById = _clients.Snapshot().ToDictionary(client => client.Id);
        var journals = await _journalEntries.ListAsync(cancellationToken: cancellationToken);
        var invoiceJournalIds = journals
            .Where(journal => journal.SourceType == JournalSourceType.BillingInvoice)
            .Where(journal => journal.SourceDocumentId.HasValue)
            .GroupBy(journal => journal.SourceDocumentId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(journal => journal.EntryDate)
                    .ThenBy(journal => journal.CreatedAtUtc)
                    .ThenBy(journal => journal.Id.Value)
                    .First()
                    .Id.Value);
        var filtered = _invoices.Snapshot()
            .Where(IsOpenInvoice)
            .Where(invoice => !request.ClientId.HasValue || invoice.ClientId.Value == request.ClientId.Value)
            .Where(invoice => !request.FromDate.HasValue || invoice.IssueDate >= request.FromDate.Value)
            .Where(invoice => !request.ToDate.HasValue || invoice.IssueDate <= request.ToDate.Value)
            .Where(invoice => request.CurrencyCode is null || string.Equals(
                invoice.CurrencyCode,
                request.CurrencyCode,
                StringComparison.Ordinal))
            .Where(invoice => clientsById.ContainsKey(invoice.ClientId))
            .Select(invoice => new InvoiceResidual(invoice, invoice.BalanceDue.Amount))
            .Where(row => row.BalanceDue > 0)
            .Where(row => !request.MinAmount.HasValue || row.BalanceDue >= request.MinAmount.Value)
            .Where(row => !request.MaxAmount.HasValue || row.BalanceDue <= request.MaxAmount.Value)
            .Where(row => MatchesStatus(row.Invoice, request.Status, request.Today))
            .Select(row => ToReadItem(
                row,
                clientsById[row.Invoice.ClientId],
                request.Today,
                invoiceJournalIds.GetValueOrDefault(row.Invoice.Id.Value)))
            .OrderByDescending(item => item.IssueDate)
            .ThenByDescending(item => item.CreatedAtUtc)
            .ThenByDescending(item => item.InvoiceId)
            .ToArray();
        var filteredCount = filtered.LongLength;

        if (request.AfterInvoiceId.HasValue)
        {
            filtered = filtered
                .Where(item => IsAfterCursor(item, request))
                .ToArray();
        }

        return new OutstandingInvoiceReadPage(filtered.Take(request.Take).ToArray(), filteredCount);
    }

    private static bool IsOpenInvoice(Invoice invoice)
    {
        return invoice.Status is InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid;
    }

    private static decimal SumBucket(
        IEnumerable<InvoiceResidual> rows,
        DateOnly asOfDate,
        int? minimumDays,
        int? maximumDays)
    {
        return rows
            .Where(row =>
            {
                var days = asOfDate.DayNumber - row.Invoice.DueDate.DayNumber;
                return (!minimumDays.HasValue || days >= minimumDays.Value)
                    && (!maximumDays.HasValue || days <= maximumDays.Value);
            })
            .Sum(row => row.BalanceDue);
    }

    private static bool MatchesStatus(Invoice invoice, string status, DateOnly today)
    {
        return status switch
        {
            "Issued" => invoice.Status == InvoiceStatus.Issued,
            "PartiallyPaid" => invoice.Status == InvoiceStatus.PartiallyPaid,
            "Overdue" => invoice.DueDate < today,
            _ => true
        };
    }

    private static OutstandingInvoiceReadItem ToReadItem(
        InvoiceResidual row,
        Domain.Modules.Clients.Client client,
        DateOnly today,
        Guid? journalEntryId)
    {
        var daysOverdue = Math.Max(today.DayNumber - row.Invoice.DueDate.DayNumber, 0);
        return new OutstandingInvoiceReadItem(
            row.Invoice.Id.Value,
            client.Id.Value,
            client.Code.Value,
            client.DisplayName,
            row.Invoice.Number.Value,
            row.Invoice.IssueDate,
            row.Invoice.DueDate,
            row.Invoice.Status.ToString(),
            row.Invoice.TotalAmount.Amount,
            row.Invoice.AmountPaid.Amount,
            row.BalanceDue,
            row.Invoice.CurrencyCode,
            daysOverdue,
            GetAgingBucket(daysOverdue),
            journalEntryId,
            row.Invoice.CreatedAtUtc);
    }

    private static string GetAgingBucket(int daysOverdue)
    {
        return daysOverdue switch
        {
            <= 0 => "Current",
            <= 30 => "1-30",
            <= 60 => "31-60",
            <= 90 => "61-90",
            _ => "91+"
        };
    }

    private static bool IsAfterCursor(
        OutstandingInvoiceReadItem item,
        OutstandingInvoiceReadRequest request)
    {
        var afterIssueDate = request.AfterIssueDate.GetValueOrDefault();
        var afterCreatedAtUtc = request.AfterCreatedAtUtc.GetValueOrDefault();
        var afterInvoiceId = request.AfterInvoiceId.GetValueOrDefault();

        return item.IssueDate < afterIssueDate
            || (item.IssueDate == afterIssueDate && item.CreatedAtUtc < afterCreatedAtUtc)
            || (item.IssueDate == afterIssueDate
                && item.CreatedAtUtc == afterCreatedAtUtc
                && item.InvoiceId.CompareTo(afterInvoiceId) < 0);
    }

    private sealed record InvoiceResidual(Invoice Invoice, decimal BalanceDue);
}
