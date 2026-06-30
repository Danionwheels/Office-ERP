namespace SafarSuite.ControlDesk.Application.Modules.Billing.GenerateInvoiceDraft;

public sealed record GenerateInvoiceDraftCommand(
    Guid ClientId,
    Guid ContractId,
    string InvoiceNumber,
    DateOnly IssueDate,
    DateOnly DueDate,
    DateOnly BillingDate,
    string CurrencyCode);
