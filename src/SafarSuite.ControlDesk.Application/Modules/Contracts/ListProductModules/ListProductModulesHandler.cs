using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts.ListProductModules;

public sealed class ListProductModulesHandler
{
    private readonly IProductModuleCatalog _catalog;
    private readonly IProductModuleReferenceReader _referenceReader;

    public ListProductModulesHandler(
        IProductModuleCatalog catalog,
        IProductModuleReferenceReader referenceReader)
    {
        _catalog = catalog;
        _referenceReader = referenceReader;
    }

    public async Task<Result<ListProductModulesResult>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var revision = await _catalog.GetPublishedRevisionAsync(cancellationToken);
        var modules = revision.Definition.Modules;
        var referencesByModule = (await _referenceReader.ListActiveAsync(cancellationToken))
            .GroupBy(reference => reference.ModuleCode, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(reference => new ProductModuleContractReferenceResult(
                        reference.ContractId,
                        reference.ContractNumber,
                        reference.ContractRevisionNumber,
                        reference.ClientId))
                    .ToArray(),
                StringComparer.Ordinal);

        return Result<ListProductModulesResult>.Success(new ListProductModulesResult(
            revision.Id.Value,
            revision.RevisionNumber,
            modules.Select(module => new ProductModuleResult(
                    module.ModuleCode.Value,
                    module.DisplayName,
                    module.Description,
                    module.CommercialMode.ToString(),
                    module.IsActive,
                    module.BillingDefaults is null ? null : new ProductModuleBillingDefaultsResult(
                        module.BillingDefaults.ChargeCode,
                        module.BillingDefaults.ChargeName,
                        module.BillingDefaults.Description,
                        module.BillingDefaults.DefaultUnitPriceAmount,
                        module.BillingDefaults.CurrencyCode,
                        module.BillingDefaults.BillingCycle.ToString()),
                    new ProductModuleCompatibilityResult(
                        module.Compatibility.MinimumSafarSuiteVersion,
                        module.Compatibility.MinimumLocalServerVersion,
                        module.Compatibility.SupportedDeploymentModes.ToArray()),
                    referencesByModule.GetValueOrDefault(module.ModuleCode.Value, [])))
                .ToArray()));
    }
}

public sealed record ListProductModulesResult(
    Guid CatalogRevisionId,
    long CatalogRevisionNumber,
    IReadOnlyCollection<ProductModuleResult> Modules);

public sealed record ProductModuleResult(
    string ModuleCode,
    string DisplayName,
    string Description,
    string CommercialMode,
    bool IsActive,
    ProductModuleBillingDefaultsResult? BillingDefaults,
    ProductModuleCompatibilityResult Compatibility,
    IReadOnlyCollection<ProductModuleContractReferenceResult> ReferencedBy);

public sealed record ProductModuleContractReferenceResult(
    Guid ContractId,
    string ContractNumber,
    long ContractRevisionNumber,
    Guid ClientId);

public sealed record ProductModuleCompatibilityResult(
    string? MinimumSafarSuiteVersion,
    string? MinimumLocalServerVersion,
    IReadOnlyCollection<string> SupportedDeploymentModes);

public sealed record ProductModuleBillingDefaultsResult(
    string ChargeCode,
    string ChargeName,
    string Description,
    decimal DefaultUnitPriceAmount,
    string CurrencyCode,
    string BillingCycle);
