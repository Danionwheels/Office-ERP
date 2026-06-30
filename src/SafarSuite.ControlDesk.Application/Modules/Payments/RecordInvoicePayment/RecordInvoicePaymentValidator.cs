using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.RecordInvoicePayment;

public sealed class RecordInvoicePaymentValidator : IValidator<RecordInvoicePaymentCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(RecordInvoicePaymentCommand value)
    {
        var errors = new List<ApplicationError>();

        if (value.InvoiceId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.InvoiceId), "Invoice id is required."));
        }

        if (string.IsNullOrWhiteSpace(value.Method))
        {
            errors.Add(ApplicationError.Validation(nameof(value.Method), "Payment method is required."));
        }

        if (string.IsNullOrWhiteSpace(value.Reference))
        {
            errors.Add(ApplicationError.Validation(nameof(value.Reference), "Payment reference is required."));
        }

        if (value.Amount <= 0)
        {
            errors.Add(ApplicationError.Validation(nameof(value.Amount), "Payment amount must be positive."));
        }

        if (string.IsNullOrWhiteSpace(value.CurrencyCode))
        {
            errors.Add(ApplicationError.Validation(nameof(value.CurrencyCode), "Currency code is required."));
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
