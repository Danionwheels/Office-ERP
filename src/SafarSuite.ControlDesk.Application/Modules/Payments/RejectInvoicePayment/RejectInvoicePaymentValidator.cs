using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.RejectInvoicePayment;

public sealed class RejectInvoicePaymentValidator : IValidator<RejectInvoicePaymentCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(RejectInvoicePaymentCommand value)
    {
        var errors = new List<ApplicationError>();

        if (value.PaymentId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.PaymentId), "Payment id is required."));
        }

        if (string.IsNullOrWhiteSpace(value.DecisionNote))
        {
            errors.Add(ApplicationError.Validation(nameof(value.DecisionNote), "Rejection note is required."));
        }

        return errors;
    }
}
