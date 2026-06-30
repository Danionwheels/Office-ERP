using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;

namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.UpdateSurveyJobInvoiceLines;

public sealed class UpdateSurveyJobInvoiceLinesValidator : IValidator<UpdateSurveyJobInvoiceLinesCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(UpdateSurveyJobInvoiceLinesCommand value)
    {
        var errors = new List<ApplicationError>();

        if (value.SurveyJobId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.SurveyJobId), "Survey job id is required."));
        }

        if (value.InvoiceLines is null)
        {
            errors.Add(ApplicationError.Validation(nameof(value.InvoiceLines), "Invoice lines are required."));
            return errors;
        }

        var duplicateSequence = value.InvoiceLines
            .GroupBy(line => line.SequenceNumber)
            .FirstOrDefault(group => group.Key > 0 && group.Count() > 1);

        if (duplicateSequence is not null)
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.InvoiceLines),
                $"Invoice line sequence {duplicateSequence.Key} is duplicated."));
        }

        foreach (var line in value.InvoiceLines)
        {
            if (line.SequenceNumber <= 0)
            {
                errors.Add(ApplicationError.Validation(nameof(line.SequenceNumber), "Invoice line sequence number must be positive."));
            }

            if (!Enum.IsDefined(line.DescriptionType))
            {
                errors.Add(ApplicationError.Validation(nameof(line.DescriptionType), "Invoice line description type is not valid."));
            }

            if (string.IsNullOrWhiteSpace(line.Description))
            {
                errors.Add(ApplicationError.Validation(nameof(line.Description), "Invoice line description is required."));
            }

            if (line.Amount < 0)
            {
                errors.Add(ApplicationError.Validation(nameof(line.Amount), "Invoice line amount cannot be negative."));
            }

            if (string.IsNullOrWhiteSpace(line.CurrencyCode))
            {
                errors.Add(ApplicationError.Validation(nameof(line.CurrencyCode), "Invoice line currency code is required."));
            }
        }

        return errors;
    }
}
