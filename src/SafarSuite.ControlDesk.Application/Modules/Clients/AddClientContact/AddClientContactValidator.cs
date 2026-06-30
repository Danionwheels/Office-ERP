using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.AddClientContact;

public sealed class AddClientContactValidator : IValidator<AddClientContactCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(AddClientContactCommand value)
    {
        var errors = new List<ApplicationError>();

        if (value.ClientId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.ClientId), "Client id is required."));
        }

        if (string.IsNullOrWhiteSpace(value.Role))
        {
            errors.Add(ApplicationError.Validation(nameof(value.Role), "Client contact role is required."));
        }

        if (string.IsNullOrWhiteSpace(value.FullName))
        {
            errors.Add(ApplicationError.Validation(nameof(value.FullName), "Client contact full name is required."));
        }

        if (string.IsNullOrWhiteSpace(value.Email) && string.IsNullOrWhiteSpace(value.Phone))
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.Email),
                "Client contact requires an email or phone."));
        }

        return errors;
    }
}
