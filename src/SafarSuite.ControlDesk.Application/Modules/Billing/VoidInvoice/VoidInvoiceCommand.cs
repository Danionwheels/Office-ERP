namespace SafarSuite.ControlDesk.Application.Modules.Billing.VoidInvoice;

public sealed record VoidInvoiceCommand(
    Guid InvoiceId,
    DateOnly VoidDate,
    string Reason);
