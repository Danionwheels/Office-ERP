namespace SafarSuite.ControlDesk.Application.Modules.Payments.ApproveInvoicePayment;

public sealed record ApproveInvoicePaymentCommand(
    Guid PaymentId,
    Guid CashOrBankAccountId,
    Guid AccountsReceivableAccountId,
    DateOnly PostingDate,
    string? DecisionNote);
