using SafarSuite.ControlDesk.Application.Common.Results;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.ApplyClientCredit;

public sealed class ApplyClientCreditValidator
{
    public IReadOnlyCollection<ApplicationError> Validate(ApplyClientCreditCommand command)
    {
        var errors = new List<ApplicationError>();

        if (command.ClientId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(command.ClientId), "Client id is required."));
        }

        if (command.InvoiceId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(command.InvoiceId), "Invoice id is required."));
        }

        if (string.IsNullOrWhiteSpace(command.Reference))
        {
            errors.Add(ApplicationError.Validation(nameof(command.Reference), "Credit application reference is required."));
        }

        if (command.Amount <= 0)
        {
            errors.Add(ApplicationError.Validation(nameof(command.Amount), "Applied credit amount must be positive."));
        }

        if (string.IsNullOrWhiteSpace(command.CurrencyCode))
        {
            errors.Add(ApplicationError.Validation(nameof(command.CurrencyCode), "Currency code is required."));
        }

        if (!string.IsNullOrWhiteSpace(command.Note) && command.Note.Trim().Length > 1000)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.Note),
                "Credit application note cannot exceed 1000 characters."));
        }

        return errors;
    }
}
