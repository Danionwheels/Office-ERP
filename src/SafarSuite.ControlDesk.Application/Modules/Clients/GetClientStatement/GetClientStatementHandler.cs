using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.GetClientStatement;

public sealed class GetClientStatementHandler
{
    private readonly IClientRepository _clients;
    private readonly IInvoiceRepository _invoices;
    private readonly IPaymentRepository _payments;
    private readonly IJournalEntryRepository _journalEntries;

    public GetClientStatementHandler(
        IClientRepository clients,
        IInvoiceRepository invoices,
        IPaymentRepository payments,
        IJournalEntryRepository journalEntries)
    {
        _clients = clients;
        _invoices = invoices;
        _payments = payments;
        _journalEntries = journalEntries;
    }

    public async Task<Result<GetClientStatementResult>> HandleAsync(
        GetClientStatementQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.ClientId == Guid.Empty)
        {
            return Result<GetClientStatementResult>.Failure(ApplicationError.Validation(
                nameof(query.ClientId),
                "Client id is required."));
        }

        if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate.Value > query.ToDate.Value)
        {
            return Result<GetClientStatementResult>.Failure(ApplicationError.Validation(
                nameof(query.FromDate),
                "From date cannot be after to date."));
        }

        var clientId = ClientId.Create(query.ClientId);
        var client = await _clients.GetByIdAsync(clientId, cancellationToken);

        if (client is null)
        {
            return Result<GetClientStatementResult>.Failure(ApplicationError.NotFound(
                nameof(query.ClientId),
                "Client was not found."));
        }

        var invoices = await _invoices.ListForClientAsync(
            clientId,
            query.FromDate,
            query.ToDate,
            cancellationToken);

        var payments = await _payments.ListForClientAsync(
            clientId,
            query.FromDate,
            query.ToDate,
            cancellationToken);

        var postedInvoices = invoices
            .Where(IsPostedReceivableInvoice)
            .ToArray();

        var invoiceNumbers = postedInvoices
            .Select(invoice => invoice.Number.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var paymentReferences = payments
            .Select(payment => payment.Reference.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var invoiceJournalEntries = await _journalEntries.ListAsync(
            sourceType: JournalSourceType.BillingInvoice,
            cancellationToken: cancellationToken);

        var paymentJournalEntries = await _journalEntries.ListAsync(
            sourceType: JournalSourceType.PaymentReceipt,
            cancellationToken: cancellationToken);

        var invoiceJournalByNumber = invoiceJournalEntries
            .Where(entry => entry.SourceReference is not null && invoiceNumbers.Contains(entry.SourceReference))
            .GroupBy(entry => entry.SourceReference!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var paymentJournalByReference = paymentJournalEntries
            .Where(entry => entry.SourceReference is not null && paymentReferences.Contains(entry.SourceReference))
            .GroupBy(entry => entry.SourceReference!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var statementLines = BuildStatementLines(
            invoices,
            payments,
            invoiceJournalByNumber,
            paymentJournalByReference);

        var journalPostings = invoiceJournalByNumber.Values
            .Concat(paymentJournalByReference.Values)
            .DistinctBy(entry => entry.Id.Value)
            .OrderBy(entry => entry.EntryDate)
            .ThenBy(entry => entry.CreatedAtUtc)
            .ThenBy(entry => entry.Id.Value)
            .Select(ToJournalPosting)
            .ToArray();

        return Result<GetClientStatementResult>.Success(new GetClientStatementResult(
            query.ClientId,
            query.FromDate,
            query.ToDate,
            BuildCurrencySummaries(invoices, payments),
            invoices.Select(invoice => ToInvoiceResult(invoice, invoiceJournalByNumber)).ToArray(),
            payments.Select(payment => ToPaymentResult(payment, paymentJournalByReference)).ToArray(),
            statementLines,
            journalPostings));
    }

    private static IReadOnlyCollection<ClientStatementCurrencySummaryResult> BuildCurrencySummaries(
        IReadOnlyCollection<Invoice> invoices,
        IReadOnlyCollection<Payment> payments)
    {
        var currencyCodes = invoices
            .Where(IsPostedReceivableInvoice)
            .Select(invoice => invoice.CurrencyCode)
            .Concat(payments.Select(payment => payment.Amount.CurrencyCode))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(currencyCode => currencyCode)
            .ToArray();

        return currencyCodes
            .Select(currencyCode =>
            {
                var currencyInvoices = invoices
                    .Where(IsPostedReceivableInvoice)
                    .Where(invoice => string.Equals(invoice.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                var currencyApprovedPayments = payments
                    .Where(payment => string.Equals(payment.Amount.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
                    .Where(payment => payment.Status == PaymentStatus.Approved)
                    .ToArray();

                return new ClientStatementCurrencySummaryResult(
                    currencyCode,
                    currencyInvoices.Sum(invoice => invoice.TotalAmount.Amount),
                    currencyApprovedPayments.Sum(payment => payment.Amount.Amount),
                    currencyInvoices.Sum(invoice => invoice.BalanceDue.Amount),
                    currencyInvoices.Length,
                    currencyInvoices.Count(invoice => invoice.BalanceDue.Amount > 0));
            })
            .ToArray();
    }

    private static IReadOnlyCollection<ClientStatementLineResult> BuildStatementLines(
        IReadOnlyCollection<Invoice> invoices,
        IReadOnlyCollection<Payment> payments,
        IReadOnlyDictionary<string, JournalEntry> invoiceJournalByNumber,
        IReadOnlyDictionary<string, JournalEntry> paymentJournalByReference)
    {
        var rawLines = invoices
            .Where(IsPostedReceivableInvoice)
            .Select(invoice =>
            {
                invoiceJournalByNumber.TryGetValue(invoice.Number.Value, out var journalEntry);

                return new RawStatementLine(
                    invoice.IssueDate,
                    1,
                    "Invoice",
                    invoice.Number.Value,
                    invoice.Id.Value,
                    null,
                    $"Invoice {invoice.Number.Value}",
                    invoice.TotalAmount.Amount,
                    0m,
                    invoice.CurrencyCode,
                    journalEntry?.Id.Value);
            })
            .Concat(payments.Select(payment =>
            {
                paymentJournalByReference.TryGetValue(payment.Reference.Value, out var journalEntry);

                return new RawStatementLine(
                    payment.ReceivedOn,
                    2,
                    "Payment",
                    payment.Reference.Value,
                    payment.InvoiceId.Value,
                    payment.Id.Value,
                    $"Payment {payment.Reference.Value} ({payment.Method})",
                    0m,
                    payment.Status == PaymentStatus.Approved ? payment.Amount.Amount : 0m,
                    payment.Amount.CurrencyCode,
                    journalEntry?.Id.Value);
            }))
            .OrderBy(line => line.EntryDate)
            .ThenBy(line => line.SortOrder)
            .ThenBy(line => line.Reference, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var runningBalances = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var statementLines = new List<ClientStatementLineResult>(rawLines.Length);

        foreach (var line in rawLines)
        {
            runningBalances.TryGetValue(line.CurrencyCode, out var currentBalance);
            currentBalance += line.Debit - line.Credit;
            runningBalances[line.CurrencyCode] = currentBalance;

            statementLines.Add(new ClientStatementLineResult(
                line.EntryDate,
                line.DocumentType,
                line.Reference,
                line.InvoiceId,
                line.PaymentId,
                line.Description,
                line.Debit,
                line.Credit,
                currentBalance,
                line.CurrencyCode,
                line.JournalEntryId));
        }

        return statementLines;
    }

    private static ClientStatementInvoiceResult ToInvoiceResult(
        Invoice invoice,
        IReadOnlyDictionary<string, JournalEntry> invoiceJournalByNumber)
    {
        invoiceJournalByNumber.TryGetValue(invoice.Number.Value, out var journalEntry);

        return new ClientStatementInvoiceResult(
            invoice.Id.Value,
            invoice.ContractId.Value,
            invoice.Number.Value,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.Status.ToString(),
            invoice.TotalAmount.Amount,
            invoice.AmountPaid.Amount,
            invoice.BalanceDue.Amount,
            invoice.CurrencyCode,
            journalEntry?.Id.Value);
    }

    private static ClientStatementPaymentResult ToPaymentResult(
        Payment payment,
        IReadOnlyDictionary<string, JournalEntry> paymentJournalByReference)
    {
        paymentJournalByReference.TryGetValue(payment.Reference.Value, out var journalEntry);

        return new ClientStatementPaymentResult(
            payment.Id.Value,
            payment.InvoiceId.Value,
            payment.Reference.Value,
            payment.Method.ToString(),
            payment.Status.ToString(),
            payment.Amount.Amount,
            payment.Amount.CurrencyCode,
            payment.ReceivedOn,
            journalEntry?.Id.Value);
    }

    private static ClientStatementJournalPostingResult ToJournalPosting(JournalEntry entry)
    {
        return new ClientStatementJournalPostingResult(
            entry.Id.Value,
            entry.EntryDate,
            entry.SourceType.ToString(),
            entry.SourceReference,
            entry.Memo,
            entry.Status.ToString(),
            entry.TotalDebit.Amount,
            entry.TotalCredit.Amount,
            entry.CurrencyCode,
            entry.Lines.Select(line => new ClientStatementJournalLineResult(
                line.LedgerAccountId.Value,
                line.Debit.Amount,
                line.Credit.Amount,
                line.Description)).ToArray());
    }

    private static bool IsPostedReceivableInvoice(Invoice invoice)
    {
        return invoice.Status is InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid or InvoiceStatus.Paid;
    }

    private sealed record RawStatementLine(
        DateOnly EntryDate,
        int SortOrder,
        string DocumentType,
        string Reference,
        Guid? InvoiceId,
        Guid? PaymentId,
        string Description,
        decimal Debit,
        decimal Credit,
        string CurrencyCode,
        Guid? JournalEntryId);
}
