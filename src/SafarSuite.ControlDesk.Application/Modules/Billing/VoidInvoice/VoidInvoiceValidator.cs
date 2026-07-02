using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.VoidInvoice;

public sealed class VoidInvoiceValidator : IValidator<VoidInvoiceCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(VoidInvoiceCommand value)
    {
        var errors = new List<ApplicationError>();

        if (value.InvoiceId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.InvoiceId), "Invoice id is required."));
        }

        if (string.IsNullOrWhiteSpace(value.Reason))
        {
            errors.Add(ApplicationError.Validation(nameof(value.Reason), "Void reason is required."));
        }

        if (value.Reason?.Length > 512)
        {
            errors.Add(ApplicationError.Validation(nameof(value.Reason), "Void reason cannot exceed 512 characters."));
        }

        return errors;
    }
}
