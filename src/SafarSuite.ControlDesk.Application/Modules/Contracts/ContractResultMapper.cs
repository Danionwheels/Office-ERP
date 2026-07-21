using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts;

internal static class ContractResultMapper
{
    public static ClientContractResult ToResult(ClientContract contract)
    {
        return new ClientContractResult(
            contract.Id.Value,
            contract.ClientId.Value,
            contract.RevisionNumber,
            contract.SupersedesContractId?.Value,
            contract.ProductCatalogRevisionId.Value,
            contract.ProductCatalogRevisionNumber,
            contract.Number.Value,
            contract.Term.StartsOn,
            contract.Term.EndsOn,
            contract.Pricing.RecurringAmount.Amount,
            contract.Pricing.RecurringAmount.CurrencyCode,
            contract.Pricing.BillingCycle.ToString(),
            contract.Pricing.BillingDayOfMonth,
            contract.DeviceAllowance.AllowedDevices,
            contract.BranchAllowance.AllowedBranches,
            contract.Status.ToString(),
            contract.CreatedAtUtc,
            contract.ActivatedAtUtc,
            contract.ApprovedBy,
            contract.ApprovalReason,
            contract.ApprovedAtUtc,
            contract.ModuleAllowances.Select(module => new ClientContractModuleResult(
                module.ModuleCode.Value,
                module.IsEnabled)).ToArray(),
            contract.UserAllowance.AllowedNamedUsers,
            contract.UserAllowance.AllowedConcurrentUsers,
            contract.FeatureLimits.Select(limit => new ClientContractFeatureLimitResult(
                limit.ModuleCode.Value,
                limit.FeatureCode.Value,
                limit.LimitValue,
                limit.Unit)).ToArray());
    }
}

public sealed record ClientContractResult(
    Guid ContractId,
    Guid ClientId,
    long RevisionNumber,
    Guid? SupersedesContractId,
    Guid ProductCatalogRevisionId,
    long ProductCatalogRevisionNumber,
    string ContractNumber,
    DateOnly StartsOn,
    DateOnly EndsOn,
    decimal RecurringAmount,
    string CurrencyCode,
    string BillingCycle,
    int BillingDayOfMonth,
    int AllowedDevices,
    int AllowedBranches,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ActivatedAtUtc,
    string ApprovedBy,
    string ApprovalReason,
    DateTimeOffset ApprovedAtUtc,
    IReadOnlyCollection<ClientContractModuleResult> Modules,
    int? AllowedNamedUsers = null,
    int? AllowedConcurrentUsers = null,
    IReadOnlyCollection<ClientContractFeatureLimitResult>? FeatureLimits = null);

public sealed record ClientContractModuleResult(
    string ModuleCode,
    bool IsEnabled);

public sealed record ClientContractFeatureLimitResult(
    string ModuleCode,
    string FeatureCode,
    long LimitValue,
    string Unit);
