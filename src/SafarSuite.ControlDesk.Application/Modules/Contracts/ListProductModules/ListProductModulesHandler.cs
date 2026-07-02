using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts.ListProductModules;

public sealed class ListProductModulesHandler
{
    private readonly IProductModuleCatalog _catalog;

    public ListProductModulesHandler(IProductModuleCatalog catalog)
    {
        _catalog = catalog;
    }

    public async Task<Result<ListProductModulesResult>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var modules = await _catalog.ListAsync(cancellationToken);

        return Result<ListProductModulesResult>.Success(new ListProductModulesResult(
            modules.Select(module => new ProductModuleResult(
                    module.ModuleCode.Value,
                    module.DisplayName,
                    module.CommercialMode.ToString(),
                    module.IsActive,
                    module.BillingDefaults is null ? null : new ProductModuleBillingDefaultsResult(
                        module.BillingDefaults.ChargeCode,
                        module.BillingDefaults.ChargeName,
                        module.BillingDefaults.Description,
                        module.BillingDefaults.DefaultUnitPriceAmount,
                        module.BillingDefaults.CurrencyCode,
                        module.BillingDefaults.BillingCycle.ToString())))
                .ToArray()));
    }
}

public sealed record ListProductModulesResult(
    IReadOnlyCollection<ProductModuleResult> Modules);

public sealed record ProductModuleResult(
    string ModuleCode,
    string DisplayName,
    string CommercialMode,
    bool IsActive,
    ProductModuleBillingDefaultsResult? BillingDefaults);

public sealed record ProductModuleBillingDefaultsResult(
    string ChargeCode,
    string ChargeName,
    string Description,
    decimal DefaultUnitPriceAmount,
    string CurrencyCode,
    string BillingCycle);
