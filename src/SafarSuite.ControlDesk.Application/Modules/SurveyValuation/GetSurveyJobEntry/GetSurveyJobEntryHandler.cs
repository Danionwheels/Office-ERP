using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.Ports;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.SurveyJobEntry;
using SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.GetSurveyJobEntry;

public sealed class GetSurveyJobEntryHandler
{
    private readonly ISurveyJobRepository _surveyJobs;

    public GetSurveyJobEntryHandler(ISurveyJobRepository surveyJobs)
    {
        _surveyJobs = surveyJobs;
    }

    public async Task<Result<SurveyJobEntryDto>> HandleAsync(
        GetSurveyJobEntryQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = Validate(query);

        if (validationErrors.Count > 0)
        {
            return Result<SurveyJobEntryDto>.Failure(validationErrors);
        }

        try
        {
            var surveyJob = await GetSurveyJobAsync(query, cancellationToken);

            return surveyJob is null
                ? Result<SurveyJobEntryDto>.Failure(ApplicationError.NotFound(
                    nameof(query.SurveyJobId),
                    "Survey job was not found."))
                : Result<SurveyJobEntryDto>.Success(SurveyJobEntryMapper.ToDto(surveyJob));
        }
        catch (ArgumentException exception)
        {
            return Result<SurveyJobEntryDto>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(query),
                exception.Message));
        }
    }

    private async Task<SurveyJob?> GetSurveyJobAsync(
        GetSurveyJobEntryQuery query,
        CancellationToken cancellationToken)
    {
        if (query.SurveyJobId.HasValue)
        {
            return await _surveyJobs.GetByIdAsync(
                SurveyJobId.Create(query.SurveyJobId.Value),
                cancellationToken);
        }

        return await _surveyJobs.GetByNumberAsync(
            SurveyJobNumber.Create(query.SurveyJobNumber!),
            cancellationToken);
    }

    private static IReadOnlyCollection<ApplicationError> Validate(GetSurveyJobEntryQuery query)
    {
        var errors = new List<ApplicationError>();
        var hasId = query.SurveyJobId.HasValue;
        var hasNumber = !string.IsNullOrWhiteSpace(query.SurveyJobNumber);

        if (!hasId && !hasNumber)
        {
            errors.Add(ApplicationError.Validation(
                nameof(query.SurveyJobId),
                "Survey job id or survey job number is required."));
        }

        if (hasId && hasNumber)
        {
            errors.Add(ApplicationError.Validation(
                nameof(query.SurveyJobNumber),
                "Use either survey job id or survey job number, not both."));
        }

        if (query.SurveyJobId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(query.SurveyJobId),
                "Survey job id cannot be empty."));
        }

        return errors;
    }
}
