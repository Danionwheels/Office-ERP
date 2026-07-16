namespace SafarSuite.ControlDesk.Application.Modules.Contracts.CreateClientContract;

public sealed record CreateClientContractResult(
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
    IReadOnlyCollection<CreateClientContractModuleResult> Modules,
    int? AllowedNamedUsers = null,
    int? AllowedConcurrentUsers = null,
    IReadOnlyCollection<CreateClientContractFeatureLimitResult>? FeatureLimits = null);

public sealed record CreateClientContractModuleResult(
    string ModuleCode,
    bool IsEnabled);

public sealed record CreateClientContractFeatureLimitResult(
    string ModuleCode,
    string FeatureCode,
    long LimitValue,
    string Unit);
