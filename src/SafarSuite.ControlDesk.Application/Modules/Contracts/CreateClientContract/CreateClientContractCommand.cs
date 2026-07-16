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
    string ApprovedBy,
    string ApprovalReason,
    IReadOnlyCollection<CreateClientContractModuleCommand> Modules,
    int? AllowedNamedUsers = null,
    int? AllowedConcurrentUsers = null,
    IReadOnlyCollection<CreateClientContractFeatureLimitCommand>? FeatureLimits = null);

public sealed record CreateClientContractModuleCommand(
    string ModuleCode,
    bool IsEnabled);

public sealed record CreateClientContractFeatureLimitCommand(
    string ModuleCode,
    string FeatureCode,
    long LimitValue,
    string Unit);
