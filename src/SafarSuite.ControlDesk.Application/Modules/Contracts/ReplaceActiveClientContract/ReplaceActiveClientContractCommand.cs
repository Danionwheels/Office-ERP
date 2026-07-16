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
    string ApprovedBy,
    string ApprovalReason,
    IReadOnlyCollection<ReplaceActiveClientContractModuleCommand> Modules,
    int? AllowedNamedUsers = null,
    int? AllowedConcurrentUsers = null,
    IReadOnlyCollection<ReplaceActiveClientContractFeatureLimitCommand>? FeatureLimits = null);

public sealed record ReplaceActiveClientContractModuleCommand(
    string ModuleCode,
    bool IsEnabled);

public sealed record ReplaceActiveClientContractFeatureLimitCommand(
    string ModuleCode,
    string FeatureCode,
    long LimitValue,
    string Unit);
