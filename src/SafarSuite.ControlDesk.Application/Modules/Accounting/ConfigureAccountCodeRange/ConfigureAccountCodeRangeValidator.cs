using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureAccountCodeRange;

public sealed class ConfigureAccountCodeRangeValidator : IValidator<ConfigureAccountCodeRangeCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(ConfigureAccountCodeRangeCommand value)
    {
        var errors = new List<ApplicationError>();

        if (string.IsNullOrWhiteSpace(value.Role))
        {
            errors.Add(ApplicationError.Validation(nameof(value.Role), "Account code range role is required."));
        }

        if (string.IsNullOrWhiteSpace(value.DisplayName))
        {
            errors.Add(ApplicationError.Validation(nameof(value.DisplayName), "Display name is required."));
        }

        AddDigitsError(errors, value.SearchPrefix, nameof(value.SearchPrefix), "Search prefix is required.");
        AddDigitsError(errors, value.RangeStart, nameof(value.RangeStart), "Range start is required.");
        AddDigitsError(errors, value.RangeEnd, nameof(value.RangeEnd), "Range end is required.");

        if (value.CodeLength is < 2 or > 32)
        {
            errors.Add(ApplicationError.Validation(nameof(value.CodeLength), "Code length must be between 2 and 32."));
        }

        if (string.IsNullOrWhiteSpace(value.AccountType))
        {
            errors.Add(ApplicationError.Validation(nameof(value.AccountType), "Account type is required."));
        }

        if (string.IsNullOrWhiteSpace(value.NormalBalance))
        {
            errors.Add(ApplicationError.Validation(nameof(value.NormalBalance), "Normal balance is required."));
        }

        if (!string.IsNullOrWhiteSpace(value.ParentCode)
            && !value.ParentCode.All(char.IsDigit))
        {
            errors.Add(ApplicationError.Validation(nameof(value.ParentCode), "Parent code must contain digits only."));
        }

        return errors;
    }

    private static void AddDigitsError(
        List<ApplicationError> errors,
        string value,
        string memberName,
        string requiredMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(ApplicationError.Validation(memberName, requiredMessage));
            return;
        }

        if (!value.Trim().All(char.IsDigit))
        {
            errors.Add(ApplicationError.Validation(memberName, $"{memberName} must contain digits only."));
        }
    }
}
