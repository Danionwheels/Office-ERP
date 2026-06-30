using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;

namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.CreateSurveyJobBillingDraft;

public sealed class CreateSurveyJobBillingDraftValidator : IValidator<CreateSurveyJobBillingDraftCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(CreateSurveyJobBillingDraftCommand value)
    {
        var errors = new List<ApplicationError>();

        if (value.SurveyJobId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.SurveyJobId), "Survey job id is required."));
        }

        if (value.ClientId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.ClientId), "Client id is required."));
        }

        if (value.ContractId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.ContractId), "Contract id is required."));
        }

        if (string.IsNullOrWhiteSpace(value.InvoiceNumber))
        {
            errors.Add(ApplicationError.Validation(nameof(value.InvoiceNumber), "Invoice number is required."));
        }

        if (value.DueDate < value.IssueDate)
        {
            errors.Add(ApplicationError.Validation(nameof(value.DueDate), "Invoice due date cannot be before issue date."));
        }

        if (string.IsNullOrWhiteSpace(value.CurrencyCode))
        {
            errors.Add(ApplicationError.Validation(nameof(value.CurrencyCode), "Currency code is required."));
        }

        return errors;
    }
}
