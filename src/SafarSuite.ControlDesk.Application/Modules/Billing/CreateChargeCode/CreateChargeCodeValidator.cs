using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.CreateChargeCode;

public sealed class CreateChargeCodeValidator : IValidator<CreateChargeCodeCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(CreateChargeCodeCommand value)
    {
        var errors = new List<ApplicationError>();

        if (string.IsNullOrWhiteSpace(value.Code))
        {
            errors.Add(ApplicationError.Validation(nameof(value.Code), "Charge code is required."));
        }

        if (string.IsNullOrWhiteSpace(value.Name))
        {
            errors.Add(ApplicationError.Validation(nameof(value.Name), "Charge code name is required."));
        }

        if (value.DefaultUnitPriceAmount < 0)
        {
            errors.Add(ApplicationError.Validation(nameof(value.DefaultUnitPriceAmount), "Default unit price cannot be negative."));
        }

        if (string.IsNullOrWhiteSpace(value.CurrencyCode))
        {
            errors.Add(ApplicationError.Validation(nameof(value.CurrencyCode), "Currency code is required."));
        }

        if (value.RevenueAccountId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.RevenueAccountId), "Revenue ledger account id is required."));
        }

        if (value.TaxAccountId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.TaxAccountId), "Tax ledger account id cannot be empty."));
        }

        return errors;
    }
}
