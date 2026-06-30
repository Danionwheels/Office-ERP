using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.IssueInvoice;

public sealed class IssueInvoiceValidator : IValidator<IssueInvoiceCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(IssueInvoiceCommand value)
    {
        var errors = new List<ApplicationError>();

        if (value.InvoiceId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.InvoiceId), "Invoice id is required."));
        }

        if (value.AccountsReceivableAccountId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.AccountsReceivableAccountId),
                "Accounts receivable ledger account id cannot be empty."));
        }

        return errors;
    }
}
