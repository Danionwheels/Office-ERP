namespace SafarSuite.ControlDesk.Domain.Modules.Billing;

public enum InvoiceStatus
{
    Draft = 0,
    Issued = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Cancelled = 4,
    Void = 5
}
