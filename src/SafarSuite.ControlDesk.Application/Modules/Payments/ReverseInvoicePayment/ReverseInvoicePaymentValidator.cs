using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.ReverseInvoicePayment;

public sealed class ReverseInvoicePaymentValidator : IValidator<ReverseInvoicePaymentCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(ReverseInvoicePaymentCommand value)
    {
        var errors = new List<ApplicationError>();

        if (value.PaymentId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.PaymentId), "Payment id is required."));
        }

        if (string.IsNullOrWhiteSpace(value.DecisionNote))
        {
            errors.Add(ApplicationError.Validation(nameof(value.DecisionNote), "Reversal note is required."));
        }

        return errors;
    }
}
