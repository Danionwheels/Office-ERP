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

        return errors;
    }
}
