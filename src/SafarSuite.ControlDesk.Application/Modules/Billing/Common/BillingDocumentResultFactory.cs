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

    public static IssueInvoiceResult ToIssueInvoiceResult(Invoice invoice, JournalEntry journalEntry)
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
            journalEntry.Lines.Select(line => new IssueInvoiceJournalLineResult(
                line.LedgerAccountId.Value,
                line.Debit.Amount,
                line.Credit.Amount,
                line.Description)).ToArray());
    }

    public static VoidInvoiceResult ToVoidInvoiceResult(
        Invoice invoice,
        JournalEntry originalJournalEntry,
        JournalEntry reversalJournalEntry)
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
            reversalJournalEntry.Lines.Select(line => new VoidInvoiceJournalLineResult(
                line.LedgerAccountId.Value,
                line.Debit.Amount,
                line.Credit.Amount,
                line.Description)).ToArray());
    }

    public static IssueCreditNoteResult ToIssueCreditNoteResult(
        CreditNote creditNote,
        Invoice invoice,
        JournalEntry journalEntry)
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
            journalEntry.Lines.Select(line => new IssueCreditNoteJournalLineResult(
                line.LedgerAccountId.Value,
                line.Debit.Amount,
                line.Credit.Amount,
                line.Description)).ToArray());
    }
}
