using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.AddClientSupportNote;

public sealed class AddClientSupportNoteValidator : IValidator<AddClientSupportNoteCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(AddClientSupportNoteCommand value)
    {
        var errors = new List<ApplicationError>();

        if (value.ClientId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.ClientId), "Client id is required."));
        }

        if (string.IsNullOrWhiteSpace(value.Text))
        {
            errors.Add(ApplicationError.Validation(nameof(value.Text), "Support note text is required."));
        }

        if (string.IsNullOrWhiteSpace(value.CreatedBy))
        {
            errors.Add(ApplicationError.Validation(nameof(value.CreatedBy), "Support note author is required."));
        }

        return errors;
    }
}
