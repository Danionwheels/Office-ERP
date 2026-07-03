using SafarSuite.ControlDesk.Application.Modules.Billing.GenerateInvoiceDraft;
using SafarSuite.ControlDesk.Application.Modules.Billing.IssueCreditNote;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.GetCreditNoteDocument;

public sealed record CreditNoteDocumentResult(
    GenerateInvoiceDraftResult Invoice,
    IssueCreditNoteResult CreditNote);
