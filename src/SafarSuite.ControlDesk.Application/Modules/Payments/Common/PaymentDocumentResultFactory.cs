using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Payments.ApproveInvoicePayment;
using SafarSuite.ControlDesk.Application.Modules.Payments.IssueClientRefund;
using SafarSuite.ControlDesk.Application.Modules.Payments.RecordInvoicePayment;
using SafarSuite.ControlDesk.Application.Modules.Payments.ReverseInvoicePayment;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.Common;

public static class PaymentDocumentResultFactory
{
    public static RecordInvoicePaymentResult ToRecordInvoicePaymentResult(
        Payment payment,
        Invoice invoice,
        JournalEntry? journalEntry,
        IReadOnlyDictionary<Guid, LedgerAccount>? ledgerAccountsById = null)
    {
        return new RecordInvoicePaymentResult(
            payment.Id.Value,
            invoice.Id.Value,
            invoice.Number.Value,
            invoice.Status.ToString(),
            payment.Status.ToString(),
            payment.Amount.Amount,
            invoice.BalanceDue.Amount,
            payment.Amount.CurrencyCode,
            journalEntry?.Id.Value,
            journalEntry?.Status.ToString(),
            journalEntry?.EntryDate,
            journalEntry?.TotalDebit.Amount ?? 0m,
            journalEntry?.TotalCredit.Amount ?? 0m,
            journalEntry is null
                ? Array.Empty<RecordInvoicePaymentJournalLineResult>()
                : journalEntry.Lines
                    .Select(line => ToRecordInvoicePaymentJournalLineResult(line, ledgerAccountsById))
                    .ToArray());
    }

    public static ReverseInvoicePaymentResult ToReverseInvoicePaymentResult(
        Payment payment,
        Invoice invoice,
        JournalEntry reversalJournalEntry,
        JournalEntry originalReceiptJournal,
        IReadOnlyDictionary<Guid, LedgerAccount>? ledgerAccountsById = null)
    {
        return new ReverseInvoicePaymentResult(
            payment.Id.Value,
            invoice.Id.Value,
            invoice.Number.Value,
            invoice.Status.ToString(),
            payment.Status.ToString(),
            payment.Amount.Amount,
            invoice.BalanceDue.Amount,
            payment.Amount.CurrencyCode,
            reversalJournalEntry.Id.Value,
            reversalJournalEntry.Status.ToString(),
            reversalJournalEntry.EntryDate,
            originalReceiptJournal.Id.Value,
            reversalJournalEntry.TotalDebit.Amount,
            reversalJournalEntry.TotalCredit.Amount,
            reversalJournalEntry.Lines
                .Select(line => ToReverseInvoicePaymentJournalLineResult(line, ledgerAccountsById))
                .ToArray());
    }

    public static IssueClientRefundResult ToIssueClientRefundResult(
        ClientRefund refund,
        JournalEntry journalEntry,
        decimal clientBalanceBefore,
        decimal clientBalanceAfter,
        IReadOnlyDictionary<Guid, LedgerAccount>? ledgerAccountsById = null)
    {
        return new IssueClientRefundResult(
            refund.Id.Value,
            refund.ClientId.Value,
            refund.Status.ToString(),
            refund.Method.ToString(),
            refund.Reference.Value,
            refund.Amount.Amount,
            clientBalanceBefore,
            clientBalanceAfter,
            refund.Amount.CurrencyCode,
            refund.RefundedOn,
            journalEntry.Id.Value,
            journalEntry.Status.ToString(),
            journalEntry.EntryDate,
            journalEntry.TotalDebit.Amount,
            journalEntry.TotalCredit.Amount,
            journalEntry.Lines
                .Select(line => ToIssueClientRefundJournalLineResult(line, ledgerAccountsById))
                .ToArray());
    }

    public static ApproveInvoicePaymentJournalLineResult ToApproveInvoicePaymentJournalLineResult(
        JournalLine line,
        IReadOnlyDictionary<Guid, LedgerAccount>? ledgerAccountsById = null)
    {
        var metadata = JournalLineLedgerAccountMetadataFactory.From(line.LedgerAccountId, ledgerAccountsById);

        return new ApproveInvoicePaymentJournalLineResult(
            line.LedgerAccountId.Value,
            line.Debit.Amount,
            line.Credit.Amount,
            line.Description,
            metadata.LedgerAccountCode,
            metadata.LedgerAccountName,
            metadata.LedgerAccountType,
            metadata.LedgerAccountNormalBalance,
            metadata.LedgerAccountLevel,
            metadata.IsPostingAccount,
            metadata.LedgerAccountStatus);
    }

    private static RecordInvoicePaymentJournalLineResult ToRecordInvoicePaymentJournalLineResult(
        JournalLine line,
        IReadOnlyDictionary<Guid, LedgerAccount>? ledgerAccountsById)
    {
        var metadata = JournalLineLedgerAccountMetadataFactory.From(line.LedgerAccountId, ledgerAccountsById);

        return new RecordInvoicePaymentJournalLineResult(
            line.LedgerAccountId.Value,
            line.Debit.Amount,
            line.Credit.Amount,
            line.Description,
            metadata.LedgerAccountCode,
            metadata.LedgerAccountName,
            metadata.LedgerAccountType,
            metadata.LedgerAccountNormalBalance,
            metadata.LedgerAccountLevel,
            metadata.IsPostingAccount,
            metadata.LedgerAccountStatus);
    }

    private static ReverseInvoicePaymentJournalLineResult ToReverseInvoicePaymentJournalLineResult(
        JournalLine line,
        IReadOnlyDictionary<Guid, LedgerAccount>? ledgerAccountsById)
    {
        var metadata = JournalLineLedgerAccountMetadataFactory.From(line.LedgerAccountId, ledgerAccountsById);

        return new ReverseInvoicePaymentJournalLineResult(
            line.LedgerAccountId.Value,
            line.Debit.Amount,
            line.Credit.Amount,
            line.Description,
            metadata.LedgerAccountCode,
            metadata.LedgerAccountName,
            metadata.LedgerAccountType,
            metadata.LedgerAccountNormalBalance,
            metadata.LedgerAccountLevel,
            metadata.IsPostingAccount,
            metadata.LedgerAccountStatus);
    }

    private static IssueClientRefundJournalLineResult ToIssueClientRefundJournalLineResult(
        JournalLine line,
        IReadOnlyDictionary<Guid, LedgerAccount>? ledgerAccountsById)
    {
        var metadata = JournalLineLedgerAccountMetadataFactory.From(line.LedgerAccountId, ledgerAccountsById);

        return new IssueClientRefundJournalLineResult(
            line.LedgerAccountId.Value,
            line.Debit.Amount,
            line.Credit.Amount,
            line.Description,
            metadata.LedgerAccountCode,
            metadata.LedgerAccountName,
            metadata.LedgerAccountType,
            metadata.LedgerAccountNormalBalance,
            metadata.LedgerAccountLevel,
            metadata.IsPostingAccount,
            metadata.LedgerAccountStatus);
    }
}
