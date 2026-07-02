using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.ApproveInvoicePayment;

public sealed class ApproveInvoicePaymentValidator : IValidator<ApproveInvoicePaymentCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(ApproveInvoicePaymentCommand value)
    {
        var errors = new List<ApplicationError>();

        if (value.PaymentId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.PaymentId), "Payment id is required."));
        }

        if (value.CashOrBankAccountId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.CashOrBankAccountId), "Cash or bank ledger account id is required."));
        }

        if (value.AccountsReceivableAccountId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.AccountsReceivableAccountId),
                "Accounts receivable ledger account id is required."));
        }

        return errors;
    }
}
