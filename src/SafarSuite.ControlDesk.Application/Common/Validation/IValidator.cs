using SafarSuite.ControlDesk.Application.Common.Results;

namespace SafarSuite.ControlDesk.Application.Common.Validation;

public interface IValidator<in T>
{
    IReadOnlyCollection<ApplicationError> Validate(T value);
}
