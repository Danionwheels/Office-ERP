using SafarSuite.ControlDesk.Application.Common.Results;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts.CreateClientContract;

public sealed class CreateClientContractValidator
{
    public IReadOnlyCollection<ApplicationError> Validate(CreateClientContractCommand command)
    {
        var errors = new List<ApplicationError>();

        if (command.ClientId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(command.ClientId), "Client id is required."));
        }

        if (string.IsNullOrWhiteSpace(command.ContractNumber))
        {
            errors.Add(ApplicationError.Validation(nameof(command.ContractNumber), "Contract number is required."));
        }

        if (command.EndsOn < command.StartsOn)
        {
            errors.Add(ApplicationError.Validation(nameof(command.EndsOn), "Contract end date cannot be before start date."));
        }

        if (command.RecurringAmount < 0)
        {
            errors.Add(ApplicationError.Validation(nameof(command.RecurringAmount), "Recurring amount cannot be negative."));
        }

        if (string.IsNullOrWhiteSpace(command.CurrencyCode))
        {
            errors.Add(ApplicationError.Validation(nameof(command.CurrencyCode), "Currency code is required."));
        }

        if (command.BillingDayOfMonth is < 1 or > 28)
        {
            errors.Add(ApplicationError.Validation(nameof(command.BillingDayOfMonth), "Billing day must be between 1 and 28."));
        }

        if (command.AllowedDevices < 0)
        {
            errors.Add(ApplicationError.Validation(nameof(command.AllowedDevices), "Allowed device count cannot be negative."));
        }

        if (command.AllowedBranches < 0)
        {
            errors.Add(ApplicationError.Validation(nameof(command.AllowedBranches), "Allowed branch count cannot be negative."));
        }

        foreach (var module in command.Modules)
        {
            if (string.IsNullOrWhiteSpace(module.ModuleCode))
            {
                errors.Add(ApplicationError.Validation(nameof(module.ModuleCode), "Module code is required."));
            }
        }

        return errors;
    }
}
