using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.SurveyJobEntry;
using SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.UpdateSurveyJob;

public sealed class UpdateSurveyJobValidator : IValidator<UpdateSurveyJobCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(UpdateSurveyJobCommand value)
    {
        var errors = new List<ApplicationError>();

        if (value.SurveyJobId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.SurveyJobId),
                "Survey job id is required."));
        }

        if (!Enum.IsDefined(typeof(SurveyJobStatus), value.Status))
        {
            errors.Add(ApplicationError.Validation(nameof(value.Status), "Survey job status is not valid."));
        }

        SurveyJobEntryValidationRules.ValidateMainFields(errors, value);

        return errors;
    }
}
