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
    private readonly ICreditNoteRepository _creditNotes;
    private readonly IPaymentRepository _payments;
    private readonly IClientRefundRepository _refunds;
    private readonly IClientCreditApplicationRepository _creditApplications;
    private readonly IJournalEntryRepository _journalEntries;
    private readonly ILedgerAccountRepository _ledgerAccounts;

    public GetClientStatementHandler(
        IClientRepository clients,
        IInvoiceRepository invoices,
        ICreditNoteRepository creditNotes,
        IPaymentRepository payments,
        IClientRefundRepository refunds,
        IClientCreditApplicationRepository creditApplications,
        IJournalEntryRepository journalEntries,
        ILedgerAccountRepository ledgerAccounts)
    {
        _clients = clients;
        _invoices = invoices;
        _creditNotes = creditNotes;
        _payments = payments;
        _refunds = refunds;
        _creditApplications = creditApplications;
        _journalEntries = journalEntries;
        _ledgerAccounts = ledgerAccounts;
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

        var creditNotes = await _creditNotes.ListForClientAsync(
            clientId,
            query.FromDate,
            query.ToDate,
            cancellationToken);

        var refunds = await _refunds.ListForClientAsync(
            clientId,
            query.FromDate,
            query.ToDate,
            cancellationToken);

        var creditApplications = await _creditApplications.ListForClientAsync(
            clientId,
            query.FromDate,
            query.ToDate,
            cancellationToken);

        var statementInvoices = invoices
            .Where(IsStatementInvoice)
            .ToArray();

        var invoiceNumbers = statementInvoices
            .Select(invoice => invoice.Number.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var paymentReferences = payments
            .Select(payment => payment.Reference.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var creditNoteNumbers = creditNotes
            .Select(creditNote => creditNote.Number.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var refundReferences = refunds
            .Select(refund => refund.Reference.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var invoiceJournalEntries = await _journalEntries.ListAsync(
            sourceType: JournalSourceType.BillingInvoice,
            cancellationToken: cancellationToken);

        var invoiceVoidJournalEntries = await _journalEntries.ListAsync(
            sourceType: JournalSourceType.BillingInvoiceVoid,
            cancellationToken: cancellationToken);

        var paymentReceiptJournalEntries = await _journalEntries.ListAsync(
            sourceType: JournalSourceType.PaymentReceipt,
            cancellationToken: cancellationToken);

        var paymentReversalJournalEntries = await _journalEntries.ListAsync(
            sourceType: JournalSourceType.PaymentReversal,
            cancellationToken: cancellationToken);

        var creditNoteJournalEntries = await _journalEntries.ListAsync(
            sourceType: JournalSourceType.BillingCreditNote,
            cancellationToken: cancellationToken);

        var refundJournalEntries = await _journalEntries.ListAsync(
            sourceType: JournalSourceType.ClientRefund,
            cancellationToken: cancellationToken);

        var invoiceJournalsByNumber = invoiceJournalEntries
            .Concat(invoiceVoidJournalEntries)
            .Where(entry => entry.SourceReference is not null && invoiceNumbers.Contains(entry.SourceReference))
            .GroupBy(entry => entry.SourceReference!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<JournalEntry>)group
                    .OrderBy(entry => entry.EntryDate)
                    .ThenBy(entry => entry.CreatedAtUtc)
                    .ThenBy(entry => entry.Id.Value)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var paymentJournalsByReference = paymentReceiptJournalEntries
            .Concat(paymentReversalJournalEntries)
            .Where(entry => entry.SourceReference is not null && paymentReferences.Contains(entry.SourceReference))
            .GroupBy(entry => entry.SourceReference!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<JournalEntry>)group
                    .OrderBy(entry => entry.EntryDate)
                    .ThenBy(entry => entry.CreatedAtUtc)
                    .ThenBy(entry => entry.Id.Value)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var creditNoteJournalByNumber = creditNoteJournalEntries
            .Where(entry => entry.SourceReference is not null && creditNoteNumbers.Contains(entry.SourceReference))
            .GroupBy(entry => entry.SourceReference!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var refundJournalByReference = refundJournalEntries
            .Where(entry => entry.SourceReference is not null && refundReferences.Contains(entry.SourceReference))
            .GroupBy(entry => entry.SourceReference!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var ledgerAccountsById = (await _ledgerAccounts.ListAsync(cancellationToken: cancellationToken))
            .ToDictionary(account => account.Id.Value);

        var statementLines = BuildStatementLines(
            invoices,
            payments,
            creditNotes,
            refunds,
            creditApplications,
            invoiceJournalsByNumber,
            paymentJournalsByReference,
            creditNoteJournalByNumber,
            refundJournalByReference);

        var journalPostings = invoiceJournalsByNumber.Values.SelectMany(entries => entries)
            .Concat(paymentJournalsByReference.Values.SelectMany(entries => entries))
            .Concat(creditNoteJournalByNumber.Values)
            .Concat(refundJournalByReference.Values)
            .DistinctBy(entry => entry.Id.Value)
            .OrderBy(entry => entry.EntryDate)
            .ThenBy(entry => entry.CreatedAtUtc)
            .ThenBy(entry => entry.Id.Value)
            .Select(entry => ToJournalPosting(entry, ledgerAccountsById))
            .ToArray();

        return Result<GetClientStatementResult>.Success(new GetClientStatementResult(
            query.ClientId,
            query.FromDate,
            query.ToDate,
            BuildCurrencySummaries(invoices, payments, creditNotes, refunds, creditApplications),
            invoices.Select(invoice => ToInvoiceResult(invoice, invoiceJournalsByNumber)).ToArray(),
            payments.Select(payment => ToPaymentResult(payment, paymentJournalsByReference)).ToArray(),
            statementLines,
            journalPostings));
    }

    private static IReadOnlyCollection<ClientStatementCurrencySummaryResult> BuildCurrencySummaries(
        IReadOnlyCollection<Invoice> invoices,
        IReadOnlyCollection<Payment> payments,
        IReadOnlyCollection<CreditNote> creditNotes,
        IReadOnlyCollection<ClientRefund> refunds,
        IReadOnlyCollection<ClientCreditApplication> creditApplications)
    {
        var currencyCodes = invoices
            .Where(IsStatementInvoice)
            .Select(invoice => invoice.CurrencyCode)
            .Concat(payments.Select(payment => payment.Amount.CurrencyCode))
            .Concat(creditNotes.Select(creditNote => creditNote.CurrencyCode))
            .Concat(refunds.Select(refund => refund.CurrencyCode))
            .Concat(creditApplications.Select(application => application.CurrencyCode))
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
                var currencyCreditNotes = creditNotes
                    .Where(creditNote => string.Equals(creditNote.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                var currencyRefunds = refunds
                    .Where(refund => string.Equals(refund.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
                    .Where(refund => refund.Status == ClientRefundStatus.Issued)
                    .ToArray();
                var currencyCreditApplications = creditApplications
                    .Where(application => string.Equals(application.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
                    .Where(application => application.Status == ClientCreditApplicationStatus.Applied)
                    .ToArray();
                var availableCredit = Math.Max(
                    currencyCreditNotes.Sum(creditNote => creditNote.TotalAmount.Amount)
                        - currencyRefunds.Sum(refund => refund.Amount.Amount)
                        - currencyCreditApplications.Sum(application => application.Amount.Amount),
                    0m);

                return new ClientStatementCurrencySummaryResult(
                    currencyCode,
                    currencyInvoices.Sum(invoice => invoice.TotalAmount.Amount),
                    currencyApprovedPayments.Sum(payment => payment.Amount.Amount),
                    availableCredit,
                    currencyInvoices.Sum(invoice => invoice.BalanceDue.Amount)
                        - currencyCreditNotes.Sum(creditNote => creditNote.TotalAmount.Amount)
                        + currencyCreditApplications.Sum(application => application.Amount.Amount)
                        + currencyRefunds.Sum(refund => refund.Amount.Amount),
                    currencyInvoices.Length,
                    currencyInvoices.Count(invoice => invoice.BalanceDue.Amount > 0));
            })
            .ToArray();
    }

    private static IReadOnlyCollection<ClientStatementLineResult> BuildStatementLines(
        IReadOnlyCollection<Invoice> invoices,
        IReadOnlyCollection<Payment> payments,
        IReadOnlyCollection<CreditNote> creditNotes,
        IReadOnlyCollection<ClientRefund> refunds,
        IReadOnlyCollection<ClientCreditApplication> creditApplications,
        IReadOnlyDictionary<string, IReadOnlyCollection<JournalEntry>> invoiceJournalsByNumber,
        IReadOnlyDictionary<string, IReadOnlyCollection<JournalEntry>> paymentJournalsByReference,
        IReadOnlyDictionary<string, JournalEntry> creditNoteJournalByNumber,
        IReadOnlyDictionary<string, JournalEntry> refundJournalByReference)
    {
        var rawLines = invoices
            .Where(IsStatementInvoice)
            .SelectMany(invoice => BuildInvoiceStatementLines(invoice, invoiceJournalsByNumber))
            .Concat(payments.SelectMany(payment => BuildPaymentStatementLines(payment, paymentJournalsByReference)))
            .Concat(creditNotes.Select(creditNote => BuildCreditNoteStatementLine(creditNote, creditNoteJournalByNumber)))
            .Concat(refunds.Select(refund => BuildRefundStatementLine(refund, refundJournalByReference)))
            .Concat(creditApplications.Select(BuildCreditApplicationStatementLine))
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
                line.RefundId,
                line.CreditApplicationId,
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
        IReadOnlyDictionary<string, IReadOnlyCollection<JournalEntry>> invoiceJournalsByNumber)
    {
        invoiceJournalsByNumber.TryGetValue(invoice.Number.Value, out var journalEntries);
        var journalEntry = journalEntries?.FirstOrDefault(entry => entry.SourceType == JournalSourceType.BillingInvoice);

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
        IReadOnlyDictionary<string, IReadOnlyCollection<JournalEntry>> paymentJournalsByReference)
    {
        paymentJournalsByReference.TryGetValue(payment.Reference.Value, out var journalEntries);
        var journalEntry = journalEntries?
            .OrderByDescending(entry => entry.EntryDate)
            .ThenByDescending(entry => entry.CreatedAtUtc)
            .FirstOrDefault();

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

    private static IEnumerable<RawStatementLine> BuildInvoiceStatementLines(
        Invoice invoice,
        IReadOnlyDictionary<string, IReadOnlyCollection<JournalEntry>> invoiceJournalsByNumber)
    {
        invoiceJournalsByNumber.TryGetValue(invoice.Number.Value, out var journalEntries);
        var invoiceJournal = journalEntries?.FirstOrDefault(entry => entry.SourceType == JournalSourceType.BillingInvoice);
        var voidJournal = journalEntries?.FirstOrDefault(entry => entry.SourceType == JournalSourceType.BillingInvoiceVoid);

        yield return new RawStatementLine(
            invoiceJournal?.EntryDate ?? invoice.IssueDate,
            1,
            "Invoice",
            invoice.Number.Value,
            invoice.Id.Value,
            null,
            null,
            null,
            $"Invoice {invoice.Number.Value}",
            invoice.TotalAmount.Amount,
            0m,
            invoice.CurrencyCode,
            invoiceJournal?.Id.Value);

        if (invoice.Status == InvoiceStatus.Void)
        {
            yield return new RawStatementLine(
                voidJournal?.EntryDate ?? invoice.IssueDate,
                2,
                "Invoice void",
                invoice.Number.Value,
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

    private static IEnumerable<RawStatementLine> BuildPaymentStatementLines(
        Payment payment,
        IReadOnlyDictionary<string, IReadOnlyCollection<JournalEntry>> paymentJournalsByReference)
    {
        paymentJournalsByReference.TryGetValue(payment.Reference.Value, out var journalEntries);
        var receiptJournal = journalEntries?.FirstOrDefault(entry => entry.SourceType == JournalSourceType.PaymentReceipt);
        var reversalJournal = journalEntries?.FirstOrDefault(entry => entry.SourceType == JournalSourceType.PaymentReversal);

        if (payment.Status == PaymentStatus.Reversed)
        {
            yield return new RawStatementLine(
                receiptJournal?.EntryDate ?? payment.ReceivedOn,
                3,
                "Payment",
                payment.Reference.Value,
                payment.InvoiceId.Value,
                payment.Id.Value,
                null,
                null,
                $"Payment {payment.Reference.Value} ({payment.Method})",
                0m,
                payment.Amount.Amount,
                payment.Amount.CurrencyCode,
                receiptJournal?.Id.Value);

            yield return new RawStatementLine(
                reversalJournal?.EntryDate ?? payment.ReceivedOn,
                4,
                "Payment reversal",
                payment.Reference.Value,
                payment.InvoiceId.Value,
                payment.Id.Value,
                null,
                null,
                $"Reversal of payment {payment.Reference.Value}",
                payment.Amount.Amount,
                0m,
                payment.Amount.CurrencyCode,
                reversalJournal?.Id.Value);

            yield break;
        }

        yield return new RawStatementLine(
            receiptJournal?.EntryDate ?? payment.ReceivedOn,
            3,
            "Payment",
            payment.Reference.Value,
            payment.InvoiceId.Value,
            payment.Id.Value,
            null,
            null,
            $"Payment {payment.Reference.Value} ({payment.Method}) - {payment.Status}",
            0m,
            payment.Status == PaymentStatus.Approved ? payment.Amount.Amount : 0m,
            payment.Amount.CurrencyCode,
            receiptJournal?.Id.Value);
    }

    private static RawStatementLine BuildCreditNoteStatementLine(
        CreditNote creditNote,
        IReadOnlyDictionary<string, JournalEntry> creditNoteJournalByNumber)
    {
        creditNoteJournalByNumber.TryGetValue(creditNote.Number.Value, out var journalEntry);

        return new RawStatementLine(
            journalEntry?.EntryDate ?? creditNote.CreditDate,
            5,
            "Credit note",
            creditNote.Number.Value,
            creditNote.InvoiceId.Value,
            null,
            null,
            null,
            $"Credit note {creditNote.Number.Value}",
            0m,
            creditNote.TotalAmount.Amount,
            creditNote.CurrencyCode,
            journalEntry?.Id.Value);
    }

    private static RawStatementLine BuildRefundStatementLine(
        ClientRefund refund,
        IReadOnlyDictionary<string, JournalEntry> refundJournalByReference)
    {
        refundJournalByReference.TryGetValue(refund.Reference.Value, out var journalEntry);

        return new RawStatementLine(
            journalEntry?.EntryDate ?? refund.RefundedOn,
            6,
            "Client refund",
            refund.Reference.Value,
            null,
            null,
            refund.Id.Value,
            null,
            $"Client refund {refund.Reference.Value} ({refund.Method})",
            refund.Amount.Amount,
            0m,
            refund.CurrencyCode,
            journalEntry?.Id.Value);
    }

    private static RawStatementLine BuildCreditApplicationStatementLine(
        ClientCreditApplication application)
    {
        return new RawStatementLine(
            application.AppliedOn,
            7,
            "Applied credit",
            application.Reference.Value,
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

    private static ClientStatementJournalPostingResult ToJournalPosting(
        JournalEntry entry,
        IReadOnlyDictionary<Guid, LedgerAccount> ledgerAccountsById)
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
            entry.Lines.Select(line =>
            {
                ledgerAccountsById.TryGetValue(line.LedgerAccountId.Value, out var ledgerAccount);

                return new ClientStatementJournalLineResult(
                    line.LedgerAccountId.Value,
                    ledgerAccount?.Code.Value,
                    ledgerAccount?.Name,
                    ledgerAccount?.Type.ToString(),
                    ledgerAccount?.Level.ToString(),
                    ledgerAccount?.IsPostingAccount,
                    ledgerAccount?.Status.ToString(),
                    line.Debit.Amount,
                    line.Credit.Amount,
                    line.Description);
            }).ToArray());
    }

    private static bool IsPostedReceivableInvoice(Invoice invoice)
    {
        return invoice.Status is InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid or InvoiceStatus.Paid;
    }

    private static bool IsStatementInvoice(Invoice invoice)
    {
        return IsPostedReceivableInvoice(invoice) || invoice.Status == InvoiceStatus.Void;
    }

    private sealed record RawStatementLine(
        DateOnly EntryDate,
        int SortOrder,
        string DocumentType,
        string Reference,
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
