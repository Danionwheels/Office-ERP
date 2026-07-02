namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Contracts;

public sealed record ListProductModulesResponse(
    IReadOnlyCollection<ProductModuleResponse> Modules);

public sealed record ProductModuleResponse(
    string ModuleCode,
    string DisplayName,
    string CommercialMode,
    bool IsActive,
    ProductModuleBillingDefaultsResponse? BillingDefaults);

public sealed record ProductModuleBillingDefaultsResponse(
    string ChargeCode,
    string ChargeName,
    string Description,
    decimal DefaultUnitPriceAmount,
    string CurrencyCode,
    string BillingCycle);
