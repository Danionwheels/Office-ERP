namespace SafarSuite.ControlDesk.Application.Modules.Billing.GenerateInvoiceDraft;

public sealed record GenerateInvoiceDraftResult(
    Guid InvoiceId,
    Guid ClientId,
    Guid ContractId,
    string InvoiceNumber,
    DateOnly IssueDate,
    DateOnly DueDate,
    DateOnly BillingDate,
    decimal TotalAmount,
    decimal BalanceDue,
    string CurrencyCode,
    string Status,
    IReadOnlyCollection<GenerateInvoiceDraftLineResult> Lines);

public sealed record GenerateInvoiceDraftLineResult(
    Guid? ChargeCodeId,
    string? ProductModuleCode,
    string LineType,
    string Description,
    decimal Amount,
    string CurrencyCode);
