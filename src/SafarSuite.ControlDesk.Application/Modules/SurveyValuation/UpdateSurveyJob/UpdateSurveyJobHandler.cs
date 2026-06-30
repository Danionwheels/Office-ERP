using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.Ports;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.SurveyJobEntry;
using SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.UpdateSurveyJob;

public sealed class UpdateSurveyJobHandler
{
    private readonly ISurveyJobRepository _surveyJobs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UpdateSurveyJobValidator _validator;

    public UpdateSurveyJobHandler(
        ISurveyJobRepository surveyJobs,
        IUnitOfWork unitOfWork,
        UpdateSurveyJobValidator validator)
    {
        _surveyJobs = surveyJobs;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<SurveyJobEntryDto>> HandleAsync(
        UpdateSurveyJobCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<SurveyJobEntryDto>.Failure(validationErrors);
        }

        try
        {
            var surveyJobId = SurveyJobId.Create(command.SurveyJobId);
            var surveyJob = await _surveyJobs.GetByIdAsync(surveyJobId, cancellationToken);

            if (surveyJob is null)
            {
                return Result<SurveyJobEntryDto>.Failure(ApplicationError.NotFound(
                    nameof(command.SurveyJobId),
                    "Survey job was not found."));
            }

            SurveyJobEntryUpdater.Apply(command, surveyJob);
            surveyJob.SetStatus(command.Status);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<SurveyJobEntryDto>.Success(SurveyJobEntryMapper.ToDto(surveyJob));
        }
        catch (ArgumentException exception)
        {
            return Result<SurveyJobEntryDto>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }
}
