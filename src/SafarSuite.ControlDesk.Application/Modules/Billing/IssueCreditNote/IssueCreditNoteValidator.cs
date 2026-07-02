using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.IssueCreditNote;

public sealed class IssueCreditNoteValidator : IValidator<IssueCreditNoteCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(IssueCreditNoteCommand value)
    {
        var errors = new List<ApplicationError>();

        if (value.InvoiceId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.InvoiceId), "Invoice id is required."));
        }

        if (string.IsNullOrWhiteSpace(value.CreditNoteNumber))
        {
            errors.Add(ApplicationError.Validation(nameof(value.CreditNoteNumber), "Credit note number is required."));
        }

        if (string.IsNullOrWhiteSpace(value.Reason))
        {
            errors.Add(ApplicationError.Validation(nameof(value.Reason), "Credit note reason is required."));
        }

        if (value.Reason?.Length > 512)
        {
            errors.Add(ApplicationError.Validation(nameof(value.Reason), "Credit note reason cannot exceed 512 characters."));
        }

        return errors;
    }
}
