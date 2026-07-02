namespace SafarSuite.ControlDesk.Application.Modules.Payments.RejectInvoicePayment;

public sealed record RejectInvoicePaymentCommand(
    Guid PaymentId,
    string DecisionNote);
