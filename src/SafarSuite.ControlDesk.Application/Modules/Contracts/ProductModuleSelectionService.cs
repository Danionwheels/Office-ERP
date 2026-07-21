using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts;

public sealed class ProductModuleSelectionService
{
    private readonly IProductModuleCatalog _catalog;

    public ProductModuleSelectionService(IProductModuleCatalog catalog)
    {
        _catalog = catalog;
    }

    public async Task<Result<ProductModuleSelectionResult>> BuildAllowancesAsync(
        IReadOnlyCollection<ProductModuleSelection> selections,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<ApplicationError>();
        var catalogRevision = await _catalog.GetPublishedRevisionAsync(cancellationToken);
        var catalog = catalogRevision.Definition.Modules;
        var activeCatalog = catalog
            .Where(module => module.IsActive)
            .ToDictionary(module => module.ModuleCode.Value, StringComparer.Ordinal);
        var allCatalog = catalog.ToDictionary(module => module.ModuleCode.Value, StringComparer.Ordinal);
        var allowances = new Dictionary<string, ModuleAllowance>(StringComparer.Ordinal);

        foreach (var selection in selections)
        {
            ModuleCode moduleCode;

            try
            {
                moduleCode = ModuleCode.Create(selection.ModuleCode);
            }
            catch (ArgumentException exception)
            {
                errors.Add(ApplicationError.Validation(
                    nameof(selection.ModuleCode),
                    exception.Message));

                continue;
            }

            if (allowances.ContainsKey(moduleCode.Value))
            {
                errors.Add(ApplicationError.Validation(
                    nameof(selections),
                    $"Module code {moduleCode.Value} is duplicated."));

                continue;
            }

            ProductModuleCatalogItem? catalogItem = null;

            if (!activeCatalog.TryGetValue(moduleCode.Value, out catalogItem))
            {
                var message = allCatalog.ContainsKey(moduleCode.Value)
                    ? $"Module code {moduleCode.Value} is not active in the product module catalog."
                    : $"Module code {moduleCode.Value} is not in the product module catalog.";

                errors.Add(ApplicationError.Validation(nameof(selections), message));

                continue;
            }

            var isIncludedForAll =
                catalogItem?.CommercialMode == ProductModuleCommercialMode.IncludedForAll;
            allowances.Add(
                moduleCode.Value,
                selection.IsEnabled || isIncludedForAll
                    ? ModuleAllowance.Enabled(moduleCode)
                    : ModuleAllowance.Disabled(moduleCode));
        }

        foreach (var module in activeCatalog.Values
                     .Where(module => module.CommercialMode == ProductModuleCommercialMode.IncludedForAll))
        {
            allowances.TryAdd(module.ModuleCode.Value, ModuleAllowance.Enabled(module.ModuleCode));
        }

        if (errors.Count > 0)
        {
            return Result<ProductModuleSelectionResult>.Failure(errors);
        }

        if (allowances.Count == 0)
        {
            return Result<ProductModuleSelectionResult>.Failure(ApplicationError.Validation(
                nameof(selections),
                "At least one module is required."));
        }

        if (!allowances.Values.Any(module => module.IsEnabled))
        {
            return Result<ProductModuleSelectionResult>.Failure(ApplicationError.Validation(
                nameof(selections),
                "At least one module must be enabled."));
        }

        return Result<ProductModuleSelectionResult>.Success(new ProductModuleSelectionResult(
            catalogRevision.Id,
            catalogRevision.RevisionNumber,
            allowances.Values
                .OrderBy(module => module.ModuleCode.Value, StringComparer.Ordinal)
                .ToArray()));
    }
}

public sealed record ProductModuleSelection(
    string ModuleCode,
    bool IsEnabled);

public sealed record ProductModuleSelectionResult(
    ProductCatalogRevisionId CatalogRevisionId,
    long CatalogRevisionNumber,
    IReadOnlyCollection<ModuleAllowance> Allowances);
