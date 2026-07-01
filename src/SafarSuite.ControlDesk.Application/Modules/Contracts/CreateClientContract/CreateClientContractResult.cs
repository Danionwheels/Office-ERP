namespace SafarSuite.ControlDesk.Application.Modules.Contracts.CreateClientContract;

public sealed record CreateClientContractResult(
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
    IReadOnlyCollection<CreateClientContractModuleResult> Modules);

public sealed record CreateClientContractModuleResult(
    string ModuleCode,
    bool IsEnabled);
