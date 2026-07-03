using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.CreateAccountingPeriod;

public sealed class CreateAccountingPeriodValidator : IValidator<CreateAccountingPeriodCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(CreateAccountingPeriodCommand value)
    {
        var errors = new List<ApplicationError>();

        if (value.StartsOn == default)
        {
            errors.Add(ApplicationError.Validation(nameof(value.StartsOn), "Accounting period start date is required."));
        }

        if (value.EndsOn == default)
        {
            errors.Add(ApplicationError.Validation(nameof(value.EndsOn), "Accounting period end date is required."));
        }

        if (value.EndsOn < value.StartsOn)
        {
            errors.Add(ApplicationError.Validation(nameof(value.EndsOn), "Accounting period end date cannot be before start date."));
        }

        return errors;
    }
}
