using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts;

internal static class ContractResultMapper
{
    public static ClientContractResult ToResult(ClientContract contract)
    {
        return new ClientContractResult(
            contract.Id.Value,
            contract.ClientId.Value,
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
            contract.ModuleAllowances.Select(module => new ClientContractModuleResult(
                module.ModuleCode.Value,
                module.IsEnabled)).ToArray());
    }
}

public sealed record ClientContractResult(
    Guid ContractId,
    Guid ClientId,
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
    IReadOnlyCollection<ClientContractModuleResult> Modules);

public sealed record ClientContractModuleResult(
    string ModuleCode,
    bool IsEnabled);
