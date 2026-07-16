using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.IssueEntitlementSnapshotFromPaidInvoice;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Application.Modules.Entitlements.IssueEntitlementSnapshotFromPaidInvoiceDefaults;

public sealed class IssueEntitlementSnapshotFromPaidInvoiceDefaultsHandler
{
    private readonly IInvoiceRepository _invoices;
    private readonly IContractRepository _contracts;
    private readonly IssueEntitlementSnapshotFromPaidInvoiceHandler _issueHandler;

    public IssueEntitlementSnapshotFromPaidInvoiceDefaultsHandler(
        IInvoiceRepository invoices,
        IContractRepository contracts,
        IssueEntitlementSnapshotFromPaidInvoiceHandler issueHandler)
    {
        _invoices = invoices;
        _contracts = contracts;
        _issueHandler = issueHandler;
    }

    public async Task<Result<IssueEntitlementSnapshotFromPaidInvoiceResult>> HandleAsync(
        IssueEntitlementSnapshotFromPaidInvoiceDefaultsCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var invoiceId = InvoiceId.Create(command.InvoiceId);
            var invoice = await _invoices.GetByIdAsync(invoiceId, cancellationToken);

            if (invoice is null)
            {
                return Result<IssueEntitlementSnapshotFromPaidInvoiceResult>.Failure(ApplicationError.NotFound(
                    nameof(command.InvoiceId),
                    "Invoice was not found."));
            }

            var contract = await _contracts.GetByIdAsync(invoice.ContractId, cancellationToken);

            if (contract is null)
            {
                return Result<IssueEntitlementSnapshotFromPaidInvoiceResult>.Failure(ApplicationError.NotFound(
                    nameof(invoice.ContractId),
                    "Contract for this invoice was not found."));
            }

            if (contract.ClientId != invoice.ClientId)
            {
                return Result<IssueEntitlementSnapshotFromPaidInvoiceResult>.Failure(ApplicationError.Validation(
                    nameof(invoice.ContractId),
                    "Invoice contract does not belong to the invoice client."));
            }

            if (contract.Status != ContractStatus.Active)
            {
                return Result<IssueEntitlementSnapshotFromPaidInvoiceResult>.Failure(ApplicationError.Validation(
                    nameof(invoice.ContractId),
                    "Entitlement defaults require an active contract."));
            }

            var issueCommand = new IssueEntitlementSnapshotFromPaidInvoiceCommand(
                invoice.Id.Value,
                contract.Term.EndsOn,
                contract.Term.EndsOn.AddDays(7),
                contract.Term.EndsOn.AddDays(14),
                contract.DeviceAllowance.AllowedDevices,
                contract.BranchAllowance.AllowedBranches,
                command.ApprovedBy,
                command.ApprovalReason,
                contract.ModuleAllowances.Select(module => new IssueEntitlementSnapshotModuleCommand(
                    module.ModuleCode.Value,
                    module.IsEnabled)).ToArray(),
                contract.UserAllowance.AllowedNamedUsers,
                contract.UserAllowance.AllowedConcurrentUsers,
                contract.FeatureLimits.Select(limit => new IssueEntitlementSnapshotFeatureLimitCommand(
                    limit.ModuleCode.Value,
                    limit.FeatureCode.Value,
                    limit.LimitValue,
                    limit.Unit)).ToArray(),
                command.EffectiveFromUtc);

            return await _issueHandler.HandleAsync(issueCommand, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return Result<IssueEntitlementSnapshotFromPaidInvoiceResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }
}
