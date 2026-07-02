namespace SafarSuite.ControlDesk.Application.Modules.Payments.ReverseInvoicePayment;

public sealed record ReverseInvoicePaymentCommand(
    Guid PaymentId,
    DateOnly ReversalDate,
    string DecisionNote);
