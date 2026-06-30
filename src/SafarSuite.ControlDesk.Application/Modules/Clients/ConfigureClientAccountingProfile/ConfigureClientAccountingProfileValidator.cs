using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.ConfigureClientAccountingProfile;

public sealed class ConfigureClientAccountingProfileValidator : IValidator<ConfigureClientAccountingProfileCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(ConfigureClientAccountingProfileCommand value)
    {
        var errors = new List<ApplicationError>();

        if (value.ClientId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.ClientId), "Client id is required."));
        }

        if (value.AccountsReceivableAccountId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.AccountsReceivableAccountId),
                "Accounts receivable ledger account id is required."));
        }

        if (string.IsNullOrWhiteSpace(value.DefaultCurrencyCode))
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.DefaultCurrencyCode),
                "Default currency code is required."));
        }
        else if (value.DefaultCurrencyCode.Trim().Length != 3)
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.DefaultCurrencyCode),
                "Default currency code must be a three-letter ISO currency code."));
        }

        return errors;
    }
}
