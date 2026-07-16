using SafarSuite.ControlDesk.Application.Common.Results;

namespace SafarSuite.ControlDesk.Application.Modules.Entitlements.IssueEntitlementSnapshotFromPaidInvoice;

public sealed class IssueEntitlementSnapshotFromPaidInvoiceValidator
{
    public IReadOnlyCollection<ApplicationError> Validate(IssueEntitlementSnapshotFromPaidInvoiceCommand command)
    {
        var errors = new List<ApplicationError>();

        if (command.InvoiceId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(command.InvoiceId), "Invoice id is required."));
        }

        if (command.GraceUntil < command.PaidUntil)
        {
            errors.Add(ApplicationError.Validation(nameof(command.GraceUntil), "Grace date cannot be before paid-until date."));
        }

        if (command.OfflineValidUntil < command.PaidUntil)
        {
            errors.Add(ApplicationError.Validation(nameof(command.OfflineValidUntil), "Offline validity cannot be before paid-until date."));
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
            errors.Add(ApplicationError.Validation(nameof(command.ApprovedBy), "Approver is required."));
        }
        else if (command.ApprovedBy.Trim().Length > 256)
        {
            errors.Add(ApplicationError.Validation(nameof(command.ApprovedBy), "Approver cannot exceed 256 characters."));
        }

        if (string.IsNullOrWhiteSpace(command.ApprovalReason))
        {
            errors.Add(ApplicationError.Validation(nameof(command.ApprovalReason), "Approval reason is required."));
        }
        else if (command.ApprovalReason.Trim().Length > 1000)
        {
            errors.Add(ApplicationError.Validation(nameof(command.ApprovalReason), "Approval reason cannot exceed 1000 characters."));
        }

        if (command.Modules.Count == 0)
        {
            errors.Add(ApplicationError.Validation(nameof(command.Modules), "At least one entitlement module is required."));
        }
        else if (!command.Modules.Any(module => module.IsEnabled))
        {
            errors.Add(ApplicationError.Validation(nameof(command.Modules), "At least one entitlement module must be enabled."));
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
