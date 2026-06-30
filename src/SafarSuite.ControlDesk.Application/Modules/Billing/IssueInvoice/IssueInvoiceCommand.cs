namespace SafarSuite.ControlDesk.Application.Modules.Billing.IssueInvoice;

public sealed record IssueInvoiceCommand(
    Guid InvoiceId,
    Guid? AccountsReceivableAccountId,
    DateOnly PostingDate);
