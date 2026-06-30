using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.UpdateClient;

public sealed class UpdateClientValidator : IValidator<UpdateClientCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(UpdateClientCommand value)
    {
        var errors = new List<ApplicationError>();

        if (value.ClientId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.ClientId), "Client id is required."));
        }

        if (string.IsNullOrWhiteSpace(value.LegalName))
        {
            errors.Add(ApplicationError.Validation(nameof(value.LegalName), "Client legal name is required."));
        }

        return errors;
    }
}
