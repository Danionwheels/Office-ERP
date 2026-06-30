using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.Ports;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.SurveyJobEntry;
using SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.UpdateSurveyJobDocuments;

public sealed class UpdateSurveyJobDocumentsHandler
{
    private readonly ISurveyJobRepository _surveyJobs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UpdateSurveyJobDocumentsValidator _validator;

    public UpdateSurveyJobDocumentsHandler(
        ISurveyJobRepository surveyJobs,
        IUnitOfWork unitOfWork,
        UpdateSurveyJobDocumentsValidator validator)
    {
        _surveyJobs = surveyJobs;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<SurveyJobEntryDto>> HandleAsync(
        UpdateSurveyJobDocumentsCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<SurveyJobEntryDto>.Failure(validationErrors);
        }

        try
        {
            var surveyJob = await _surveyJobs.GetByIdAsync(
                SurveyJobId.Create(command.SurveyJobId),
                cancellationToken);

            if (surveyJob is null)
            {
                return Result<SurveyJobEntryDto>.Failure(ApplicationError.NotFound(
                    nameof(command.SurveyJobId),
                    "Survey job was not found."));
            }

            var documents = command.Documents!
                .Select(document => SurveyDocumentChecklistItem.Create(
                    document.Type,
                    document.Status,
                    document.ReceivedOn))
                .ToArray();

            surveyJob.ReplaceDocuments(documents);

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
