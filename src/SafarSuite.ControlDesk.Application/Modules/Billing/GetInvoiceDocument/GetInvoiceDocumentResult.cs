using SafarSuite.ControlDesk.Application.Modules.Billing.GenerateInvoiceDraft;
using SafarSuite.ControlDesk.Application.Modules.Billing.IssueCreditNote;
using SafarSuite.ControlDesk.Application.Modules.Billing.IssueInvoice;
using SafarSuite.ControlDesk.Application.Modules.Billing.VoidInvoice;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.GetInvoiceDocument;

public sealed record InvoiceDocumentResult(
    GenerateInvoiceDraftResult Invoice,
    IssueInvoiceResult? IssuedInvoice,
    VoidInvoiceResult? VoidedInvoice,
    IssueCreditNoteResult? CreditNote);
