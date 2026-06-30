namespace SafarSuite.ControlDesk.Application.Modules.Payments.RecordInvoicePayment;

public sealed record RecordInvoicePaymentCommand(
    Guid InvoiceId,
    string Method,
    string Reference,
    decimal Amount,
    string CurrencyCode,
    DateOnly ReceivedOn,
    Guid CashOrBankAccountId,
    Guid AccountsReceivableAccountId,
    DateOnly PostingDate);
