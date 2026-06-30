using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.SurveyJobEntry;

namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.CreateSurveyJob;

public sealed class CreateSurveyJobValidator : IValidator<CreateSurveyJobCommand>
{
    private const int SurveyJobNumberMaxLength = 32;

    public IReadOnlyCollection<ApplicationError> Validate(CreateSurveyJobCommand value)
    {
        var errors = new List<ApplicationError>();

        if (string.IsNullOrWhiteSpace(value.SurveyJobNumber))
        {
            errors.Add(ApplicationError.Validation(nameof(value.SurveyJobNumber), "Survey job number is required."));
        }
        else if (value.SurveyJobNumber.Trim().Length > SurveyJobNumberMaxLength)
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.SurveyJobNumber),
                $"Survey job number cannot exceed {SurveyJobNumberMaxLength} characters."));
        }

        SurveyJobEntryValidationRules.ValidateMainFields(errors, value);

        return errors;
    }
}
