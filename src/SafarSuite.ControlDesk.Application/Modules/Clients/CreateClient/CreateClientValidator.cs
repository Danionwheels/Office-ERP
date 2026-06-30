using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.CreateClient;

public sealed class CreateClientValidator : IValidator<CreateClientCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(CreateClientCommand value)
    {
        var errors = new List<ApplicationError>();

        if (string.IsNullOrWhiteSpace(value.Code))
        {
            errors.Add(ApplicationError.Validation(nameof(value.Code), "Client code is required."));
        }

        if (string.IsNullOrWhiteSpace(value.LegalName))
        {
            errors.Add(ApplicationError.Validation(nameof(value.LegalName), "Client legal name is required."));
        }

        return errors;
    }
}
