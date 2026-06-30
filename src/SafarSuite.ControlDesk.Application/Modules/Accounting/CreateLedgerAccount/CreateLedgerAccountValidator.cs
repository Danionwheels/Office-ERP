using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.CreateLedgerAccount;

public sealed class CreateLedgerAccountValidator : IValidator<CreateLedgerAccountCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(CreateLedgerAccountCommand value)
    {
        var errors = new List<ApplicationError>();

        if (string.IsNullOrWhiteSpace(value.Code))
        {
            errors.Add(ApplicationError.Validation(nameof(value.Code), "Ledger account code is required."));
        }

        if (string.IsNullOrWhiteSpace(value.Name))
        {
            errors.Add(ApplicationError.Validation(nameof(value.Name), "Ledger account name is required."));
        }

        if (string.IsNullOrWhiteSpace(value.Type))
        {
            errors.Add(ApplicationError.Validation(nameof(value.Type), "Ledger account type is required."));
        }

        if (string.IsNullOrWhiteSpace(value.NormalBalance))
        {
            errors.Add(ApplicationError.Validation(nameof(value.NormalBalance), "Normal balance is required."));
        }

        if (value.ParentAccountId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.ParentAccountId), "Parent ledger account id cannot be empty."));
        }

        return errors;
    }
}
