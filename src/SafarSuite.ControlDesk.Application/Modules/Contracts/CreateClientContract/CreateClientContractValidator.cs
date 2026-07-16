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

        if (command.AllowedNamedUsers < 0)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.AllowedNamedUsers),
                "Allowed named-user count cannot be negative."));
        }

        if (command.AllowedConcurrentUsers < 0)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.AllowedConcurrentUsers),
                "Allowed concurrent-user count cannot be negative."));
        }

        if (command.AllowedNamedUsers.HasValue
            && command.AllowedConcurrentUsers.HasValue
            && command.AllowedConcurrentUsers.Value > command.AllowedNamedUsers.Value)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.AllowedConcurrentUsers),
                "Allowed concurrent-user count cannot exceed the named-user count."));
        }

        if (string.IsNullOrWhiteSpace(command.ApprovedBy))
        {
            errors.Add(ApplicationError.Validation(nameof(command.ApprovedBy), "Contract approver is required."));
        }
        else if (command.ApprovedBy.Trim().Length > 256)
        {
            errors.Add(ApplicationError.Validation(nameof(command.ApprovedBy), "Contract approver cannot exceed 256 characters."));
        }

        if (string.IsNullOrWhiteSpace(command.ApprovalReason))
        {
            errors.Add(ApplicationError.Validation(nameof(command.ApprovalReason), "Contract approval reason is required."));
        }
        else if (command.ApprovalReason.Trim().Length > 1000)
        {
            errors.Add(ApplicationError.Validation(nameof(command.ApprovalReason), "Contract approval reason cannot exceed 1000 characters."));
        }

        foreach (var module in command.Modules)
        {
            if (string.IsNullOrWhiteSpace(module.ModuleCode))
            {
                errors.Add(ApplicationError.Validation(nameof(module.ModuleCode), "Module code is required."));
            }
        }

        foreach (var limit in command.FeatureLimits ?? [])
        {
            if (string.IsNullOrWhiteSpace(limit.ModuleCode))
            {
                errors.Add(ApplicationError.Validation(nameof(limit.ModuleCode), "Feature-limit module code is required."));
            }

            if (string.IsNullOrWhiteSpace(limit.FeatureCode))
            {
                errors.Add(ApplicationError.Validation(nameof(limit.FeatureCode), "Feature code is required."));
            }

            if (limit.LimitValue < 0)
            {
                errors.Add(ApplicationError.Validation(nameof(limit.LimitValue), "Feature limit value cannot be negative."));
            }

            if (string.IsNullOrWhiteSpace(limit.Unit))
            {
                errors.Add(ApplicationError.Validation(nameof(limit.Unit), "Feature limit unit is required."));
            }
        }

        return errors;
    }
}
