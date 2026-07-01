namespace SafarSuite.ControlDesk.Application.Modules.Contracts.CreateClientContract;

public sealed record CreateClientContractCommand(
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
    IReadOnlyCollection<CreateClientContractModuleCommand> Modules);

public sealed record CreateClientContractModuleCommand(
    string ModuleCode,
    bool IsEnabled);
