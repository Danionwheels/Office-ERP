namespace SafarSuite.ControlDesk.Application.Modules.Billing.IssueCreditNote;

public sealed record IssueCreditNoteCommand(
    Guid InvoiceId,
    string CreditNoteNumber,
    DateOnly CreditDate,
    string Reason);
