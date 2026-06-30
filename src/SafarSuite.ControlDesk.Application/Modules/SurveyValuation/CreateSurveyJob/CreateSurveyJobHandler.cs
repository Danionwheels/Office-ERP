using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.Ports;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.SurveyJobEntry;
using SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.CreateSurveyJob;

public sealed class CreateSurveyJobHandler
{
    private readonly ISurveyJobRepository _surveyJobs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly CreateSurveyJobValidator _validator;

    public CreateSurveyJobHandler(
        ISurveyJobRepository surveyJobs,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        CreateSurveyJobValidator validator)
    {
        _surveyJobs = surveyJobs;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<CreateSurveyJobResult>> HandleAsync(
        CreateSurveyJobCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<CreateSurveyJobResult>.Failure(validationErrors);
        }

        try
        {
            var surveyJobNumber = SurveyJobNumber.Create(command.SurveyJobNumber);

            if (await _surveyJobs.ExistsByNumberAsync(surveyJobNumber, cancellationToken))
            {
                return Result<CreateSurveyJobResult>.Failure(ApplicationError.Conflict(
                    nameof(command.SurveyJobNumber),
                    $"Survey job {surveyJobNumber.Value} already exists."));
            }

            var surveyJob = CreateSurveyJob(command, surveyJobNumber);

            await _surveyJobs.AddAsync(surveyJob, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<CreateSurveyJobResult>.Success(new CreateSurveyJobResult(
                surveyJob.Id.Value,
                surveyJob.Number.Value,
                surveyJob.Status.ToString()));
        }
        catch (ArgumentException exception)
        {
            return Result<CreateSurveyJobResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }

    private SurveyJob CreateSurveyJob(CreateSurveyJobCommand command, SurveyJobNumber surveyJobNumber)
    {
        var surveyTypeCode = SurveyReferenceCode.Create(command.SurveyTypeCode);
        var dates = SurveyJobDates.Create(
            command.IntimationDate,
            command.DeliveredDate,
            command.ReInspectionDate,
            command.InvoiceDate,
            command.VoucherDate,
            command.DiscountDate,
            command.PurchaseOrderDate);

        var surveyJob = SurveyJob.Create(
            SurveyJobId.Create(_idGenerator.NewGuid()),
            surveyJobNumber,
            surveyTypeCode,
            dates,
            _clock.UtcNow);

        SurveyJobEntryUpdater.Apply(command, surveyJob);

        return surveyJob;
    }
}
