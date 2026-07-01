namespace SafarSuite.ControlDesk.Application.Modules.Contracts.ReplaceActiveClientContract;

public sealed record ReplaceActiveClientContractCommand(
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
    IReadOnlyCollection<ReplaceActiveClientContractModuleCommand> Modules);

public sealed record ReplaceActiveClientContractModuleCommand(
    string ModuleCode,
    bool IsEnabled);
