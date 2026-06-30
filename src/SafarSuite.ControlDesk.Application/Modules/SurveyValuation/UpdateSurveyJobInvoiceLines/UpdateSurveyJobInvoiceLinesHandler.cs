using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.Ports;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.SurveyJobEntry;
using SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.UpdateSurveyJobInvoiceLines;

public sealed class UpdateSurveyJobInvoiceLinesHandler
{
    private readonly ISurveyJobRepository _surveyJobs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UpdateSurveyJobInvoiceLinesValidator _validator;

    public UpdateSurveyJobInvoiceLinesHandler(
        ISurveyJobRepository surveyJobs,
        IUnitOfWork unitOfWork,
        UpdateSurveyJobInvoiceLinesValidator validator)
    {
        _surveyJobs = surveyJobs;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<SurveyJobEntryDto>> HandleAsync(
        UpdateSurveyJobInvoiceLinesCommand command,
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

            var lines = command.InvoiceLines!
                .OrderBy(line => line.SequenceNumber)
                .Select(line => SurveyJobInvoiceLine.Create(
                    line.SequenceNumber,
                    line.DescriptionType,
                    line.Description,
                    Money.Of(line.Amount, line.CurrencyCode),
                    SurveyReferenceCode.Optional(line.BillingHeadCode),
                    SurveyReferenceCode.Optional(line.TaxCode),
                    SurveyReferenceCode.Optional(line.CategoryCode)))
                .ToArray();

            surveyJob.ReplaceInvoiceLines(lines);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<SurveyJobEntryDto>.Success(SurveyJobEntryMapper.ToDto(surveyJob));
        }
        catch (ArgumentException exception)
        {
            return Result<SurveyJobEntryDto>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<SurveyJobEntryDto>.Failure(ApplicationError.Validation(
                nameof(command),
                exception.Message));
        }
    }
}
