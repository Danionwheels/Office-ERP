using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.UpdateLedgerAccount;

public sealed class UpdateLedgerAccountValidator : IValidator<UpdateLedgerAccountCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(UpdateLedgerAccountCommand value)
    {
        var errors = new List<ApplicationError>();

        if (value.LedgerAccountId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.LedgerAccountId),
                "Ledger account id is required."));
        }

        if (string.IsNullOrWhiteSpace(value.Name))
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.Name),
                "Ledger account name is required."));
        }

        if (string.IsNullOrWhiteSpace(value.Status))
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.Status),
                "Ledger account status is required."));
        }

        return errors;
    }
}
