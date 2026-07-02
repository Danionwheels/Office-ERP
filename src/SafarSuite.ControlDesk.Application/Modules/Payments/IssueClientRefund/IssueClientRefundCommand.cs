namespace SafarSuite.ControlDesk.Application.Modules.Payments.IssueClientRefund;

public sealed record IssueClientRefundCommand(
    Guid ClientId,
    string Method,
    string Reference,
    decimal Amount,
    string CurrencyCode,
    DateOnly RefundedOn,
    Guid CashOrBankAccountId,
    Guid AccountsReceivableAccountId,
    DateOnly PostingDate,
    string? Note);
