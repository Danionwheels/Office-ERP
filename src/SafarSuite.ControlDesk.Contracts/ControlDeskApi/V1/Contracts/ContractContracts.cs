namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Contracts;

public sealed record CreateClientContractRequest(
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
    string ApprovalReason,
    IReadOnlyCollection<ClientContractModuleRequest> Modules,
    int? AllowedNamedUsers = null,
    int? AllowedConcurrentUsers = null,
    IReadOnlyCollection<ClientContractFeatureLimitRequest>? FeatureLimits = null);

public sealed record ClientContractModuleRequest(
    string ModuleCode,
    bool IsEnabled);

public sealed record ClientContractFeatureLimitRequest(
    string ModuleCode,
    string FeatureCode,
    long LimitValue,
    string Unit);

public sealed record CreateClientContractResponse(
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
    IReadOnlyCollection<ClientContractModuleResponse> Modules,
    int? AllowedNamedUsers = null,
    int? AllowedConcurrentUsers = null,
    IReadOnlyCollection<ClientContractFeatureLimitResponse>? FeatureLimits = null);

public sealed record ClientContractModuleResponse(
    string ModuleCode,
    bool IsEnabled);

public sealed record ClientContractFeatureLimitResponse(
    string ModuleCode,
    string FeatureCode,
    long LimitValue,
    string Unit);

public sealed record ClientContractResponse(
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
    IReadOnlyCollection<ClientContractModuleResponse> Modules,
    int? AllowedNamedUsers = null,
    int? AllowedConcurrentUsers = null,
    IReadOnlyCollection<ClientContractFeatureLimitResponse>? FeatureLimits = null);

public sealed record ListClientContractsResponse(
    Guid ClientId,
    IReadOnlyCollection<ClientContractResponse> Contracts);

public sealed record ReplaceActiveClientContractResponse(
    ClientContractResponse? SuspendedContract,
    ClientContractResponse ActiveContract);
