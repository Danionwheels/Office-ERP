using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Billing.GenerateInvoiceDraft;
using SafarSuite.ControlDesk.Application.Modules.Billing.IssueCreditNote;
using SafarSuite.ControlDesk.Application.Modules.Billing.IssueInvoice;
using SafarSuite.ControlDesk.Application.Modules.Billing.VoidInvoice;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Billing;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.Common;

public static class BillingDocumentResultFactory
{
    public static GenerateInvoiceDraftResult ToInvoiceDraftResult(Invoice invoice)
    {
        return new GenerateInvoiceDraftResult(
            invoice.Id.Value,
            invoice.ClientId.Value,
            invoice.ContractId.Value,
            invoice.Number.Value,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.IssueDate,
            invoice.TotalAmount.Amount,
            invoice.BalanceDue.Amount,
            invoice.CurrencyCode,
            invoice.Status.ToString(),
            invoice.Lines.Select(line => new GenerateInvoiceDraftLineResult(
                line.ChargeCodeId?.Value,
                line.ProductModuleCode?.Value,
                line.LineType.ToString(),
                line.Description,
                line.Amount.Amount,
                line.Amount.CurrencyCode)).ToArray());
    }

    public static IssueInvoiceResult ToIssueInvoiceResult(
        Invoice invoice,
        JournalEntry journalEntry,
        IReadOnlyDictionary<Guid, LedgerAccount>? ledgerAccountsById = null)
    {
        return new IssueInvoiceResult(
            invoice.Id.Value,
            invoice.Number.Value,
            invoice.Status.ToString(),
            journalEntry.Id.Value,
            journalEntry.Status.ToString(),
            journalEntry.EntryDate,
            journalEntry.TotalDebit.Amount,
            journalEntry.TotalCredit.Amount,
            journalEntry.CurrencyCode,
            journalEntry.Lines
                .Select(line => ToIssueInvoiceJournalLineResult(line, ledgerAccountsById))
                .ToArray());
    }

    public static VoidInvoiceResult ToVoidInvoiceResult(
        Invoice invoice,
        JournalEntry originalJournalEntry,
        JournalEntry reversalJournalEntry,
        IReadOnlyDictionary<Guid, LedgerAccount>? ledgerAccountsById = null)
    {
        return new VoidInvoiceResult(
            invoice.Id.Value,
            invoice.Number.Value,
            invoice.Status.ToString(),
            originalJournalEntry.Id.Value,
            reversalJournalEntry.Id.Value,
            reversalJournalEntry.Status.ToString(),
            reversalJournalEntry.EntryDate,
            reversalJournalEntry.TotalDebit.Amount,
            reversalJournalEntry.TotalCredit.Amount,
            reversalJournalEntry.CurrencyCode,
            reversalJournalEntry.Lines
                .Select(line => ToVoidInvoiceJournalLineResult(line, ledgerAccountsById))
                .ToArray());
    }

    public static IssueCreditNoteResult ToIssueCreditNoteResult(
        CreditNote creditNote,
        Invoice invoice,
        JournalEntry journalEntry,
        IReadOnlyDictionary<Guid, LedgerAccount>? ledgerAccountsById = null)
    {
        return new IssueCreditNoteResult(
            creditNote.Id.Value,
            invoice.Id.Value,
            creditNote.Number.Value,
            invoice.Number.Value,
            creditNote.Status.ToString(),
            creditNote.CreditDate,
            creditNote.TotalAmount.Amount,
            creditNote.CurrencyCode,
            journalEntry.Id.Value,
            journalEntry.Status.ToString(),
            journalEntry.TotalDebit.Amount,
            journalEntry.TotalCredit.Amount,
            journalEntry.Lines
                .Select(line => ToIssueCreditNoteJournalLineResult(line, ledgerAccountsById))
                .ToArray());
    }

    private static IssueInvoiceJournalLineResult ToIssueInvoiceJournalLineResult(
        JournalLine line,
        IReadOnlyDictionary<Guid, LedgerAccount>? ledgerAccountsById)
    {
        var metadata = JournalLineLedgerAccountMetadataFactory.From(line.LedgerAccountId, ledgerAccountsById);

        return new IssueInvoiceJournalLineResult(
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

    private static VoidInvoiceJournalLineResult ToVoidInvoiceJournalLineResult(
        JournalLine line,
        IReadOnlyDictionary<Guid, LedgerAccount>? ledgerAccountsById)
    {
        var metadata = JournalLineLedgerAccountMetadataFactory.From(line.LedgerAccountId, ledgerAccountsById);

        return new VoidInvoiceJournalLineResult(
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

    private static IssueCreditNoteJournalLineResult ToIssueCreditNoteJournalLineResult(
        JournalLine line,
        IReadOnlyDictionary<Guid, LedgerAccount>? ledgerAccountsById)
    {
        var metadata = JournalLineLedgerAccountMetadataFactory.From(line.LedgerAccountId, ledgerAccountsById);

        return new IssueCreditNoteJournalLineResult(
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
