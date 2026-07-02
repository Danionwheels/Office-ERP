namespace SafarSuite.ControlDesk.Application.Modules.Payments.RejectInvoicePayment;

public sealed record RejectInvoicePaymentResult(
    Guid PaymentId,
    Guid InvoiceId,
    string PaymentStatus,
    string? DecisionNote);
