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
    IReadOnlyCollection<ClientContractModuleRequest> Modules);

public sealed record ClientContractModuleRequest(
    string ModuleCode,
    bool IsEnabled);

public sealed record CreateClientContractResponse(
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
    IReadOnlyCollection<ClientContractModuleResponse> Modules);

public sealed record ClientContractModuleResponse(
    string ModuleCode,
    bool IsEnabled);

public sealed record ClientContractResponse(
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
    IReadOnlyCollection<ClientContractModuleResponse> Modules);

public sealed record ListClientContractsResponse(
    Guid ClientId,
    IReadOnlyCollection<ClientContractResponse> Contracts);

public sealed record ReplaceActiveClientContractResponse(
    ClientContractResponse? SuspendedContract,
    ClientContractResponse ActiveContract);
