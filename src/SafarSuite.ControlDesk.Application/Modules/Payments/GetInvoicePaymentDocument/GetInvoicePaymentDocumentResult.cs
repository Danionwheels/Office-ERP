using SafarSuite.ControlDesk.Application.Modules.Billing.GenerateInvoiceDraft;
using SafarSuite.ControlDesk.Application.Modules.Payments.RecordInvoicePayment;
using SafarSuite.ControlDesk.Application.Modules.Payments.ReverseInvoicePayment;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.GetInvoicePaymentDocument;

public sealed record InvoicePaymentDocumentResult(
    GenerateInvoiceDraftResult Invoice,
    RecordInvoicePaymentResult Payment,
    ReverseInvoicePaymentResult? Reversal);
