using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryClientFinancialReader : IClientFinancialReader
{
    private readonly IClientRepository _clients;
    private readonly IInvoiceRepository _invoices;
    private readonly IPaymentRepository _payments;
    private readonly ICreditNoteRepository _creditNotes;
    private readonly IClientRefundRepository _refunds;
    private readonly IClientCreditApplicationRepository _creditApplications;
    private readonly IJournalEntryRepository _journalEntries;

    public InMemoryClientFinancialReader(
        IClientRepository clients,
        IInvoiceRepository invoices,
        IPaymentRepository payments,
        ICreditNoteRepository creditNotes,
        IClientRefundRepository refunds,
        IClientCreditApplicationRepository creditApplications,
        IJournalEntryRepository journalEntries)
    {
        _clients = clients;
        _invoices = invoices;
        _payments = payments;
        _creditNotes = creditNotes;
        _refunds = refunds;
        _creditApplications = creditApplications;
        _journalEntries = journalEntries;
    }

    public async Task<bool> ClientExistsAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default)
    {
        return await _clients.GetByIdAsync(clientId, cancellationToken) is not null;
    }

    public async Task<ClientFinancialSummaryReadModel> ReadSummaryAsync(
        ClientId clientId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        var invoices = await _invoices.ListForClientAsync(clientId, fromDate, toDate, cancellationToken);
        var payments = await _payments.ListForClientAsync(clientId, fromDate, toDate, cancellationToken);
        var creditNotes = await _creditNotes.ListForClientAsync(clientId, fromDate, toDate, cancellationToken);
        var refunds = await _refunds.ListForClientAsync(clientId, fromDate, toDate, cancellationToken);
        var applications = await _creditApplications.ListForClientAsync(
            clientId,
            fromDate,
            toDate,
            cancellationToken);
        var currencyCodes = invoices
            .Where(IsStatementInvoice)
            .Select(invoice => invoice.CurrencyCode)
            .Concat(payments.Select(payment => payment.Amount.CurrencyCode))
            .Concat(creditNotes.Select(note => note.CurrencyCode))
            .Concat(refunds.Select(refund => refund.CurrencyCode))
            .Concat(applications.Select(application => application.CurrencyCode))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ClientFinancialSummaryReadModel(currencyCodes.Select(currencyCode =>
        {
            var postedInvoices = invoices
                .Where(IsPostedReceivableInvoice)
                .Where(invoice => SameCurrency(invoice.CurrencyCode, currencyCode))
                .ToArray();
            var approvedPayments = payments
                .Where(payment => payment.Status == PaymentStatus.Approved)
                .Where(payment => SameCurrency(payment.Amount.CurrencyCode, currencyCode))
                .ToArray();
            var currencyCredits = creditNotes
                .Where(note => SameCurrency(note.CurrencyCode, currencyCode))
                .ToArray();
            var issuedRefunds = refunds
                .Where(refund => refund.Status == ClientRefundStatus.Issued)
                .Where(refund => SameCurrency(refund.CurrencyCode, currencyCode))
                .ToArray();
            var appliedCredits = applications
                .Where(application => application.Status == ClientCreditApplicationStatus.Applied)
                .Where(application => SameCurrency(application.CurrencyCode, currencyCode))
                .ToArray();
            var creditAmount = currencyCredits.Sum(note => note.TotalAmount.Amount);
            var refundAmount = issuedRefunds.Sum(refund => refund.Amount.Amount);
            var applicationAmount = appliedCredits.Sum(application => application.Amount.Amount);

            return new ClientFinancialCurrencySummaryReadModel(
                currencyCode,
                postedInvoices.Sum(invoice => invoice.TotalAmount.Amount),
                approvedPayments.Sum(payment => payment.Amount.Amount),
                Math.Max(creditAmount - refundAmount - applicationAmount, 0m),
                postedInvoices.Sum(invoice => invoice.BalanceDue.Amount)
                    - creditAmount
                    + applicationAmount
                    + refundAmount,
                postedInvoices.LongLength,
                postedInvoices.LongCount(invoice => invoice.BalanceDue.Amount > 0));
        }).ToArray());
    }

    public async Task<ClientInvoiceReadPage> ReadInvoicePageAsync(
        ClientInvoiceReadRequest request,
        CancellationToken cancellationToken = default)
    {
        var invoices = await _invoices.ListForClientAsync(
            request.ClientId,
            request.FromDate,
            request.ToDate,
            cancellationToken);
        var journals = await ReadClientJournalsAsync(request.ClientId, cancellationToken);
        var filtered = invoices
            .Where(invoice => MatchesInvoiceState(invoice, request.State))
            .Where(invoice => request.Search.Length == 0
                || invoice.Number.Value.Contains(request.Search, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(invoice => invoice.IssueDate)
            .ThenByDescending(invoice => invoice.CreatedAtUtc)
            .ThenByDescending(invoice => invoice.Id.Value)
            .ToArray();
        var page = filtered
            .Where(invoice => IsAfterInvoiceCursor(invoice, request))
            .Take(request.Take)
            .Select(invoice => new ClientInvoiceReadItem(
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
                FindJournal(journals, JournalSourceType.BillingInvoice, invoice.Id.Value, newest: false)?.Id.Value,
                invoice.CreatedAtUtc))
            .ToArray();

        return new ClientInvoiceReadPage(page, filtered.LongLength);
    }

    public async Task<ClientPaymentReadPage> ReadPaymentPageAsync(
        ClientPaymentReadRequest request,
        CancellationToken cancellationToken = default)
    {
        var payments = await _payments.ListForClientAsync(
            request.ClientId,
            request.FromDate,
            request.ToDate,
            cancellationToken);
        var journals = await ReadClientJournalsAsync(request.ClientId, cancellationToken);
        var filtered = payments
            .Where(payment => request.Status is null
                || string.Equals(payment.Status.ToString(), request.Status, StringComparison.Ordinal))
            .Where(payment => request.Search.Length == 0
                || payment.Reference.Value.Contains(request.Search, StringComparison.OrdinalIgnoreCase)
                || payment.Method.ToString().Contains(request.Search, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(payment => payment.ReceivedOn)
            .ThenByDescending(payment => payment.RecordedAtUtc)
            .ThenByDescending(payment => payment.Id.Value)
            .ToArray();
        var page = filtered
            .Where(payment => IsAfterPaymentCursor(payment, request))
            .Take(request.Take)
            .Select(payment => new ClientPaymentReadItem(
                payment.Id.Value,
                payment.InvoiceId.Value,
                payment.Reference.Value,
                payment.Method.ToString(),
                payment.Status.ToString(),
                payment.Amount.Amount,
                payment.Amount.CurrencyCode,
                payment.ReceivedOn,
                FindLatestPaymentJournal(journals, payment.Id.Value)?.Id.Value,
                payment.RecordedAtUtc))
            .ToArray();

        return new ClientPaymentReadPage(page, filtered.LongLength);
    }

    public async Task<ClientFinancialActivityReadPage> ReadActivityPageAsync(
        ClientFinancialActivityReadRequest request,
        CancellationToken cancellationToken = default)
    {
        var invoices = await _invoices.ListForClientAsync(
            request.ClientId,
            request.FromDate,
            request.ToDate,
            cancellationToken);
        var payments = await _payments.ListForClientAsync(
            request.ClientId,
            request.FromDate,
            request.ToDate,
            cancellationToken);
        var creditNotes = await _creditNotes.ListForClientAsync(
            request.ClientId,
            request.FromDate,
            request.ToDate,
            cancellationToken);
        var refunds = await _refunds.ListForClientAsync(
            request.ClientId,
            request.FromDate,
            request.ToDate,
            cancellationToken);
        var applications = await _creditApplications.ListForClientAsync(
            request.ClientId,
            request.FromDate,
            request.ToDate,
            cancellationToken);
        var journals = await ReadClientJournalsAsync(request.ClientId, cancellationToken);
        var raw = BuildActivity(invoices, payments, creditNotes, refunds, applications, journals)
            .OrderBy(item => item.EntryDate)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Reference, StringComparer.Ordinal)
            .ThenBy(item => item.DocumentId)
            .ToArray();
        var balances = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var balanced = new List<ClientFinancialActivityReadItem>(raw.Length);

        foreach (var item in raw)
        {
            balances.TryGetValue(item.CurrencyCode, out var runningBalance);
            runningBalance += item.Debit - item.Credit;
            balances[item.CurrencyCode] = runningBalance;
            balanced.Add(new ClientFinancialActivityReadItem(
                item.EntryDate,
                item.SortOrder,
                item.DocumentType,
                item.Reference,
                item.DocumentId,
                item.InvoiceId,
                item.PaymentId,
                item.RefundId,
                item.CreditApplicationId,
                item.Description,
                item.Debit,
                item.Credit,
                runningBalance,
                item.CurrencyCode,
                item.JournalEntryId));
        }

        var filtered = balanced
            .Where(item => request.Search.Length == 0
                || item.Reference.Contains(request.Search, StringComparison.OrdinalIgnoreCase)
                || item.DocumentType.Contains(request.Search, StringComparison.OrdinalIgnoreCase)
                || item.Description.Contains(request.Search, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.EntryDate)
            .ThenByDescending(item => item.SortOrder)
            .ThenByDescending(item => item.Reference, StringComparer.Ordinal)
            .ThenByDescending(item => item.DocumentId)
            .ToArray();
        var page = filtered
            .Where(item => IsAfterActivityCursor(item, request))
            .Take(request.Take)
            .ToArray();

        return new ClientFinancialActivityReadPage(page, filtered.LongLength);
    }

    public async Task<ClientJournalPostingReadPage> ReadJournalPageAsync(
        ClientJournalPostingReadRequest request,
        CancellationToken cancellationToken = default)
    {
        var journals = await _journalEntries.ListAsync(
            request.FromDate,
            request.ToDate,
            cancellationToken: cancellationToken);
        var filtered = journals
            .Where(journal => journal.ClientId == request.ClientId)
            .Where(journal => request.SourceType is null
                || string.Equals(journal.SourceType.ToString(), request.SourceType, StringComparison.Ordinal))
            .Where(journal => request.Search.Length == 0
                || (journal.SourceReference?.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (journal.Memo?.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ?? false)
                || journal.SourceType.ToString().Contains(request.Search, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(journal => journal.EntryDate)
            .ThenByDescending(journal => journal.CreatedAtUtc)
            .ThenByDescending(journal => journal.Id.Value)
            .ToArray();
        var page = filtered
            .Where(journal => IsAfterJournalCursor(journal, request))
            .Take(request.Take)
            .Select(journal => new ClientJournalPostingReadItem(
                journal.Id.Value,
                journal.EntryDate,
                journal.SourceType.ToString(),
                journal.SourceReference,
                journal.Memo,
                journal.Status.ToString(),
                journal.TotalDebit.Amount,
                journal.TotalCredit.Amount,
                journal.CurrencyCode,
                journal.Lines.Count,
                journal.CreatedAtUtc))
            .ToArray();

        return new ClientJournalPostingReadPage(page, filtered.LongLength);
    }

    public async Task<ClientCreditBalanceReadModel> ReadCreditBalanceAsync(
        ClientId clientId,
        string currencyCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedCurrency = currencyCode.Trim().ToUpperInvariant();
        var invoices = await _invoices.ListForClientAsync(clientId, cancellationToken: cancellationToken);
        var creditNotes = await _creditNotes.ListForClientAsync(clientId, cancellationToken: cancellationToken);
        var refunds = await _refunds.ListForClientAsync(clientId, cancellationToken: cancellationToken);
        var applications = await _creditApplications.ListForClientAsync(
            clientId,
            cancellationToken: cancellationToken);

        return new ClientCreditBalanceReadModel(
            normalizedCurrency,
            invoices
                .Where(IsPostedReceivableInvoice)
                .Where(invoice => SameCurrency(invoice.CurrencyCode, normalizedCurrency))
                .Sum(invoice => invoice.BalanceDue.Amount),
            creditNotes
                .Where(note => SameCurrency(note.CurrencyCode, normalizedCurrency))
                .Sum(note => note.TotalAmount.Amount),
            refunds
                .Where(refund => refund.Status == ClientRefundStatus.Issued)
                .Where(refund => SameCurrency(refund.CurrencyCode, normalizedCurrency))
                .Sum(refund => refund.Amount.Amount),
            applications
                .Where(application => application.Status == ClientCreditApplicationStatus.Applied)
                .Where(application => SameCurrency(application.CurrencyCode, normalizedCurrency))
                .Sum(application => application.Amount.Amount));
    }

    private async Task<JournalEntry[]> ReadClientJournalsAsync(
        ClientId clientId,
        CancellationToken cancellationToken)
    {
        var journals = await _journalEntries.ListAsync(cancellationToken: cancellationToken);
        return journals.Where(journal => journal.ClientId == clientId).ToArray();
    }

    private static IEnumerable<RawActivity> BuildActivity(
        IReadOnlyCollection<Invoice> invoices,
        IReadOnlyCollection<Payment> payments,
        IReadOnlyCollection<CreditNote> creditNotes,
        IReadOnlyCollection<ClientRefund> refunds,
        IReadOnlyCollection<ClientCreditApplication> applications,
        IReadOnlyCollection<JournalEntry> journals)
    {
        foreach (var invoice in invoices.Where(IsStatementInvoice))
        {
            var issueJournal = FindJournal(
                journals,
                JournalSourceType.BillingInvoice,
                invoice.Id.Value,
                newest: false);
            yield return new RawActivity(
                issueJournal?.EntryDate ?? invoice.IssueDate,
                1,
                "Invoice",
                invoice.Number.Value,
                invoice.Id.Value,
                invoice.Id.Value,
                null,
                null,
                null,
                $"Invoice {invoice.Number.Value}",
                invoice.TotalAmount.Amount,
                0m,
                invoice.CurrencyCode,
                issueJournal?.Id.Value);

            if (invoice.Status == InvoiceStatus.Void)
            {
                var voidJournal = FindJournal(
                    journals,
                    JournalSourceType.BillingInvoiceVoid,
                    invoice.Id.Value,
                    newest: false);
                yield return new RawActivity(
                    voidJournal?.EntryDate ?? invoice.IssueDate,
                    2,
                    "Invoice void",
                    invoice.Number.Value,
                    invoice.Id.Value,
                    invoice.Id.Value,
                    null,
                    null,
                    null,
                    $"Void invoice {invoice.Number.Value}",
                    0m,
                    invoice.TotalAmount.Amount,
                    invoice.CurrencyCode,
                    voidJournal?.Id.Value);
            }
        }

        foreach (var payment in payments)
        {
            var receiptJournal = FindJournal(
                journals,
                JournalSourceType.PaymentReceipt,
                payment.Id.Value,
                newest: false);
            yield return new RawActivity(
                receiptJournal?.EntryDate ?? payment.ReceivedOn,
                3,
                "Payment",
                payment.Reference.Value,
                payment.Id.Value,
                payment.InvoiceId.Value,
                payment.Id.Value,
                null,
                null,
                $"Payment {payment.Reference.Value} ({payment.Method}) - {payment.Status}",
                0m,
                payment.Status is PaymentStatus.Approved or PaymentStatus.Reversed
                    ? payment.Amount.Amount
                    : 0m,
                payment.Amount.CurrencyCode,
                receiptJournal?.Id.Value);

            if (payment.Status == PaymentStatus.Reversed)
            {
                var reversalJournal = FindJournal(
                    journals,
                    JournalSourceType.PaymentReversal,
                    payment.Id.Value,
                    newest: false);
                yield return new RawActivity(
                    reversalJournal?.EntryDate ?? payment.ReceivedOn,
                    4,
                    "Payment reversal",
                    payment.Reference.Value,
                    payment.Id.Value,
                    payment.InvoiceId.Value,
                    payment.Id.Value,
                    null,
                    null,
                    $"Reversal of payment {payment.Reference.Value}",
                    payment.Amount.Amount,
                    0m,
                    payment.Amount.CurrencyCode,
                    reversalJournal?.Id.Value);
            }
        }

        foreach (var note in creditNotes)
        {
            var journal = FindJournal(
                journals,
                JournalSourceType.BillingCreditNote,
                note.Id.Value,
                newest: false);
            yield return new RawActivity(
                journal?.EntryDate ?? note.CreditDate,
                5,
                "Credit note",
                note.Number.Value,
                note.Id.Value,
                note.InvoiceId.Value,
                null,
                null,
                null,
                $"Credit note {note.Number.Value}",
                0m,
                note.TotalAmount.Amount,
                note.CurrencyCode,
                journal?.Id.Value);
        }

        foreach (var refund in refunds)
        {
            var journal = FindJournal(
                journals,
                JournalSourceType.ClientRefund,
                refund.Id.Value,
                newest: false);
            yield return new RawActivity(
                journal?.EntryDate ?? refund.RefundedOn,
                6,
                "Client refund",
                refund.Reference.Value,
                refund.Id.Value,
                null,
                null,
                refund.Id.Value,
                null,
                $"Client refund {refund.Reference.Value} ({refund.Method})",
                refund.Amount.Amount,
                0m,
                refund.CurrencyCode,
                journal?.Id.Value);
        }

        foreach (var application in applications)
        {
            yield return new RawActivity(
                application.AppliedOn,
                7,
                "Applied credit",
                application.Reference.Value,
                application.Id.Value,
                application.InvoiceId.Value,
                null,
                null,
                application.Id.Value,
                $"Applied credit {application.Reference.Value}",
                application.Amount.Amount,
                application.Amount.Amount,
                application.CurrencyCode,
                null);
        }
    }

    private static JournalEntry? FindLatestPaymentJournal(
        IEnumerable<JournalEntry> journals,
        Guid paymentId)
    {
        return journals
            .Where(journal => journal.SourceDocumentId == paymentId)
            .Where(journal => journal.SourceType is JournalSourceType.PaymentReceipt
                or JournalSourceType.PaymentReversal)
            .OrderByDescending(journal => journal.EntryDate)
            .ThenByDescending(journal => journal.CreatedAtUtc)
            .ThenByDescending(journal => journal.Id.Value)
            .FirstOrDefault();
    }

    private static JournalEntry? FindJournal(
        IEnumerable<JournalEntry> journals,
        JournalSourceType sourceType,
        Guid sourceDocumentId,
        bool newest)
    {
        var matches = journals
            .Where(journal => journal.SourceType == sourceType)
            .Where(journal => journal.SourceDocumentId == sourceDocumentId);

        return newest
            ? matches
                .OrderByDescending(journal => journal.EntryDate)
                .ThenByDescending(journal => journal.CreatedAtUtc)
                .ThenByDescending(journal => journal.Id.Value)
                .FirstOrDefault()
            : matches
                .OrderBy(journal => journal.EntryDate)
                .ThenBy(journal => journal.CreatedAtUtc)
                .ThenBy(journal => journal.Id.Value)
                .FirstOrDefault();
    }

    private static bool IsAfterInvoiceCursor(Invoice invoice, ClientInvoiceReadRequest request)
    {
        if (!request.AfterInvoiceId.HasValue)
        {
            return true;
        }

        return invoice.IssueDate < request.AfterIssueDate
            || invoice.IssueDate == request.AfterIssueDate
            && (invoice.CreatedAtUtc < request.AfterCreatedAtUtc
                || invoice.CreatedAtUtc == request.AfterCreatedAtUtc
                && invoice.Id.Value.CompareTo(request.AfterInvoiceId.Value) < 0);
    }

    private static bool IsAfterPaymentCursor(Payment payment, ClientPaymentReadRequest request)
    {
        if (!request.AfterPaymentId.HasValue)
        {
            return true;
        }

        return payment.ReceivedOn < request.AfterReceivedOn
            || payment.ReceivedOn == request.AfterReceivedOn
            && (payment.RecordedAtUtc < request.AfterRecordedAtUtc
                || payment.RecordedAtUtc == request.AfterRecordedAtUtc
                && payment.Id.Value.CompareTo(request.AfterPaymentId.Value) < 0);
    }

    private static bool IsAfterActivityCursor(
        ClientFinancialActivityReadItem item,
        ClientFinancialActivityReadRequest request)
    {
        if (!request.AfterDocumentId.HasValue)
        {
            return true;
        }

        return item.EntryDate < request.AfterEntryDate
            || item.EntryDate == request.AfterEntryDate
            && (item.SortOrder < request.AfterSortOrder
                || item.SortOrder == request.AfterSortOrder
                && (string.CompareOrdinal(item.Reference, request.AfterReference) < 0
                    || string.Equals(item.Reference, request.AfterReference, StringComparison.Ordinal)
                    && item.DocumentId.CompareTo(request.AfterDocumentId.Value) < 0));
    }

    private static bool IsAfterJournalCursor(
        JournalEntry journal,
        ClientJournalPostingReadRequest request)
    {
        if (!request.AfterJournalEntryId.HasValue)
        {
            return true;
        }

        return journal.EntryDate < request.AfterEntryDate
            || journal.EntryDate == request.AfterEntryDate
            && (journal.CreatedAtUtc < request.AfterCreatedAtUtc
                || journal.CreatedAtUtc == request.AfterCreatedAtUtc
                && journal.Id.Value.CompareTo(request.AfterJournalEntryId.Value) < 0);
    }

    private static bool MatchesInvoiceState(Invoice invoice, ClientInvoiceRegisterState state)
    {
        return state switch
        {
            ClientInvoiceRegisterState.Open => invoice.Status is InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid
                && invoice.BalanceDue.Amount > 0,
            ClientInvoiceRegisterState.Paid => invoice.Status == InvoiceStatus.Paid,
            ClientInvoiceRegisterState.Draft => invoice.Status == InvoiceStatus.Draft,
            ClientInvoiceRegisterState.Void => invoice.Status == InvoiceStatus.Void,
            _ => true
        };
    }

    private static bool IsPostedReceivableInvoice(Invoice invoice)
    {
        return invoice.Status is InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid or InvoiceStatus.Paid;
    }

    private static bool IsStatementInvoice(Invoice invoice)
    {
        return IsPostedReceivableInvoice(invoice) || invoice.Status == InvoiceStatus.Void;
    }

    private static bool SameCurrency(string first, string second)
    {
        return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record RawActivity(
        DateOnly EntryDate,
        int SortOrder,
        string DocumentType,
        string Reference,
        Guid DocumentId,
        Guid? InvoiceId,
        Guid? PaymentId,
        Guid? RefundId,
        Guid? CreditApplicationId,
        string Description,
        decimal Debit,
        decimal Credit,
        string CurrencyCode,
        Guid? JournalEntryId);
}
