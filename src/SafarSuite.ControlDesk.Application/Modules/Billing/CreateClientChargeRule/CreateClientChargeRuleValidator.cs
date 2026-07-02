using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.CreateClientChargeRule;

public sealed class CreateClientChargeRuleValidator : IValidator<CreateClientChargeRuleCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(CreateClientChargeRuleCommand value)
    {
        var errors = new List<ApplicationError>();

        if (value.ClientId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.ClientId), "Client id is required."));
        }

        if (value.ContractId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.ContractId), "Contract id cannot be empty."));
        }

        if (value.ChargeCodeId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.ChargeCodeId), "Charge code id is required."));
        }

        if (value.ProductModuleCode is not null && value.ProductModuleCode.Length > 64)
        {
            errors.Add(ApplicationError.Validation(nameof(value.ProductModuleCode), "Product module code cannot exceed 64 characters."));
        }

        if (value.UnitPriceAmount < 0)
        {
            errors.Add(ApplicationError.Validation(nameof(value.UnitPriceAmount), "Unit price cannot be negative."));
        }

        if (string.IsNullOrWhiteSpace(value.CurrencyCode))
        {
            errors.Add(ApplicationError.Validation(nameof(value.CurrencyCode), "Currency code is required."));
        }

        if (value.Quantity <= 0)
        {
            errors.Add(ApplicationError.Validation(nameof(value.Quantity), "Quantity must be positive."));
        }

        if (value.TaxPercent is < 0 or > 100)
        {
            errors.Add(ApplicationError.Validation(nameof(value.TaxPercent), "Tax percent must be between 0 and 100."));
        }

        if (string.IsNullOrWhiteSpace(value.BillingCycle))
        {
            errors.Add(ApplicationError.Validation(nameof(value.BillingCycle), "Billing cycle is required."));
        }

        if (value.BillingDayOfMonth is < 1 or > 28)
        {
            errors.Add(ApplicationError.Validation(nameof(value.BillingDayOfMonth), "Billing day must be between 1 and 28."));
        }

        if (value.EffectiveEndsOn < value.EffectiveStartsOn)
        {
            errors.Add(ApplicationError.Validation(nameof(value.EffectiveEndsOn), "Effective end date cannot be before start date."));
        }

        return errors;
    }
}
