using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts.SaveProductAccessCatalog;

public sealed class SaveProductAccessCatalogHandler
{
    private readonly IProductModuleCatalog _catalog;
    private readonly IProductCatalogRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;

    public SaveProductAccessCatalogHandler(
        IProductModuleCatalog catalog,
        IProductCatalogRepository repository,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock)
    {
        _catalog = catalog;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
    }

    public async Task<Result<ProductCatalogSnapshotResult>> HandleAsync(
        SaveProductAccessCatalogCommand command,
        CancellationToken cancellationToken = default)
    {
        var requestedBy = NormalizeText(command.RequestedBy, 160);
        var changeReason = NormalizeText(command.ChangeReason, 1000);
        var errors = Validate(requestedBy, changeReason);

        if (errors.Count > 0)
        {
            return Result<ProductCatalogSnapshotResult>.Failure(errors);
        }

        try
        {
            await _catalog.GetPublishedRevisionAsync(cancellationToken);

            var draft = await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var latest = await _repository.GetLatestPublishedForUpdateAsync(token)
                        ?? throw new InvalidOperationException("Published product catalog was not found.");
                    var currentDraft = await _repository.GetDraftAsync(token);
                    var currentDefinition = currentDraft?.Definition ?? latest.Definition;
                    var modules = command.Modules is null
                        ? currentDefinition.Modules
                        : NormalizeModules(command.Modules);
                    ValidateModuleMutation(currentDefinition.Modules, modules);
                    var accessCatalog = NormalizeAccessCatalog(command.ModuleGroups, command.Resources);
                    var definition = ProductCatalogDefinition.Create(modules, accessCatalog);
                    var saved = ProductCatalogDraft.Save(
                        currentDraft?.DraftId ?? _idGenerator.NewGuid(),
                        latest.Id,
                        latest.RevisionNumber,
                        definition,
                        changeReason!,
                        requestedBy!,
                        _clock.UtcNow);

                    await _repository.SaveDraftAsync(saved, token);

                    return saved;
                },
                cancellationToken);

            return Result<ProductCatalogSnapshotResult>.Success(
                ProductCatalogSnapshotResultMapper.FromDraft(draft));
        }
        catch (ArgumentException exception)
        {
            return Result<ProductCatalogSnapshotResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<ProductCatalogSnapshotResult>.Failure(ApplicationError.Validation(
                nameof(command),
                exception.Message));
        }
    }

    private static IReadOnlyCollection<ApplicationError> Validate(
        string? requestedBy,
        string? changeReason)
    {
        var errors = new List<ApplicationError>();

        if (requestedBy is null)
        {
            errors.Add(ApplicationError.Validation(
                nameof(SaveProductAccessCatalogCommand.RequestedBy),
                "Requested by is required before saving a product catalog draft."));
        }

        if (changeReason is null)
        {
            errors.Add(ApplicationError.Validation(
                nameof(SaveProductAccessCatalogCommand.ChangeReason),
                "Change reason is required before saving a product catalog draft."));
        }

        return errors;
    }

    private static IReadOnlyCollection<ProductModuleCatalogItem> NormalizeModules(
        IReadOnlyCollection<SaveProductModuleCommand> commands)
    {
        return commands.Select(command =>
        {
            if (!Enum.TryParse<ProductModuleCommercialMode>(
                    command.CommercialMode,
                    ignoreCase: true,
                    out var commercialMode)
                || !Enum.IsDefined(commercialMode))
            {
                throw new InvalidOperationException(
                    $"Product module {command.ModuleCode} has unsupported commercial mode {command.CommercialMode}.");
            }

            ProductModuleBillingDefaults? billingDefaults = null;

            if (command.BillingDefaults is not null)
            {
                if (!Enum.TryParse<BillingCycle>(
                        command.BillingDefaults.BillingCycle,
                        ignoreCase: true,
                        out var billingCycle)
                    || !Enum.IsDefined(billingCycle))
                {
                    throw new InvalidOperationException(
                        $"Product module {command.ModuleCode} has unsupported billing cycle {command.BillingDefaults.BillingCycle}.");
                }

                billingDefaults = ProductModuleBillingDefaults.Create(
                    command.BillingDefaults.ChargeCode,
                    command.BillingDefaults.ChargeName,
                    command.BillingDefaults.Description,
                    command.BillingDefaults.DefaultUnitPriceAmount,
                    command.BillingDefaults.CurrencyCode,
                    billingCycle);
            }

            return ProductModuleCatalogItem.Create(
                command.ModuleCode,
                command.DisplayName,
                commercialMode,
                command.IsActive,
                billingDefaults,
                ProductModuleCompatibility.Create(
                    command.Compatibility.MinimumSafarSuiteVersion,
                    command.Compatibility.MinimumLocalServerVersion,
                    command.Compatibility.SupportedDeploymentModes),
                command.Description);
        }).ToArray();
    }

    private static void ValidateModuleMutation(
        IReadOnlyCollection<ProductModuleCatalogItem> currentModules,
        IReadOnlyCollection<ProductModuleCatalogItem> nextModules)
    {
        var nextLookup = nextModules.ToLookup(
            module => module.ModuleCode.Value,
            StringComparer.Ordinal);

        foreach (var currentModule in currentModules)
        {
            var nextModule = nextLookup[currentModule.ModuleCode.Value].FirstOrDefault();

            if (nextModule is null)
            {
                throw new InvalidOperationException(
                    $"Product module {currentModule.ModuleCode.Value} cannot be deleted or renamed. Deactivate it instead.");
            }

            if (nextModule.CommercialMode != currentModule.CommercialMode)
            {
                throw new InvalidOperationException(
                    $"Product module {currentModule.ModuleCode.Value} commercial mode is immutable.");
            }
        }
    }

    private static ProductAccessCatalog NormalizeAccessCatalog(
        IReadOnlyCollection<SaveProductModuleGroupCommand> groupCommands,
        IReadOnlyCollection<SaveProductResourceCommand> resourceCommands)
    {
        var groups = groupCommands.Select(NormalizeGroup).ToArray();
        var groupLookup = new Dictionary<string, ProductModuleGroupCatalogItem>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            if (!groupLookup.TryAdd(group.GroupId, group))
            {
                throw new InvalidOperationException(
                    $"Product access catalog contains duplicate group id {group.GroupId}.");
            }
        }

        var resources = resourceCommands
            .Select(resource => NormalizeResource(resource, groupLookup))
            .ToArray();

        return new ProductAccessCatalog(groups, resources);
    }

    private static ProductModuleGroupCatalogItem NormalizeGroup(
        SaveProductModuleGroupCommand command)
    {
        var groupId = NormalizeRequiredText(command.GroupId, "Product access catalog group id is required.");
        var displayName = NormalizeRequiredText(
            command.DisplayName,
            $"Product access catalog group {groupId} display name is required.");
        var moduleCodes = NormalizeIds(command.ModuleCodes);

        if (moduleCodes.Count == 0)
        {
            throw new InvalidOperationException(
                $"Product access catalog group {groupId} must include at least one module code.");
        }

        return new ProductModuleGroupCatalogItem(
            groupId,
            displayName,
            NormalizeAccessKind(command.AccessKind),
            moduleCodes);
    }

    private static ProductResourceCatalogItem NormalizeResource(
        SaveProductResourceCommand command,
        IReadOnlyDictionary<string, ProductModuleGroupCatalogItem> groupLookup)
    {
        var resourceId = NormalizeRequiredText(command.ResourceId, "Product resource id is required.");
        var displayName = NormalizeRequiredText(
            command.DisplayName,
            $"Product resource {resourceId} display name is required.");
        var accessKind = NormalizeAccessKind(command.AccessKind);
        var requiredGroupIds = NormalizeIds(command.RequiredGroupIds);
        var requiredModuleCodes = NormalizeIds(command.RequiredModuleCodes);
        var resolvedModuleCodes = new List<string>();
        var resolvedLookup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var moduleCode in requiredModuleCodes)
        {
            if (resolvedLookup.Add(moduleCode))
            {
                resolvedModuleCodes.Add(moduleCode);
            }
        }

        foreach (var groupId in requiredGroupIds)
        {
            if (!groupLookup.TryGetValue(groupId, out var group))
            {
                throw new InvalidOperationException(
                    $"Product resource {resourceId} references unknown group {groupId}.");
            }

            foreach (var moduleCode in group.ModuleCodes)
            {
                if (resolvedLookup.Add(moduleCode))
                {
                    resolvedModuleCodes.Add(moduleCode);
                }
            }
        }

        if (!string.Equals(accessKind, ProductAccessKinds.Public, StringComparison.Ordinal)
            && resolvedModuleCodes.Count == 0)
        {
            throw new InvalidOperationException(
                $"Product resource {resourceId} must resolve to at least one module code.");
        }

        return new ProductResourceCatalogItem(
            resourceId,
            displayName,
            accessKind,
            requiredGroupIds,
            requiredModuleCodes,
            resolvedModuleCodes);
    }

    private static IReadOnlyCollection<string> NormalizeIds(IEnumerable<string> values)
    {
        var ids = new List<string>();
        var lookup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values)
        {
            var normalized = value.Trim();

            if (normalized.Length > 0 && lookup.Add(normalized))
            {
                ids.Add(normalized);
            }
        }

        return ids;
    }

    private static string NormalizeRequiredText(string value, string message)
    {
        var normalized = value.Trim();
        return normalized.Length > 0 ? normalized : throw new InvalidOperationException(message);
    }

    private static string NormalizeAccessKind(string accessKind)
    {
        return accessKind.Trim().ToLowerInvariant() switch
        {
            "public" => ProductAccessKinds.Public,
            "coreincluded" or "core_included" or "core-included" => ProductAccessKinds.CoreIncluded,
            "paidmodule" or "paid_module" or "paid-add-on" or "paid-addon" or "paid-module" => ProductAccessKinds.PaidModule,
            _ => throw new InvalidOperationException(
                $"Product access catalog access kind {accessKind} is not supported.")
        };
    }

    private static string? NormalizeText(string value, int maxLength)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}

public sealed record SaveProductAccessCatalogCommand(
    IReadOnlyCollection<SaveProductModuleCommand>? Modules,
    IReadOnlyCollection<SaveProductModuleGroupCommand> ModuleGroups,
    IReadOnlyCollection<SaveProductResourceCommand> Resources,
    string ChangeReason,
    string RequestedBy);

public sealed record SaveProductModuleCommand(
    string ModuleCode,
    string DisplayName,
    string Description,
    string CommercialMode,
    bool IsActive,
    SaveProductModuleBillingDefaultsCommand? BillingDefaults,
    SaveProductModuleCompatibilityCommand Compatibility);

public sealed record SaveProductModuleBillingDefaultsCommand(
    string ChargeCode,
    string ChargeName,
    string Description,
    decimal DefaultUnitPriceAmount,
    string CurrencyCode,
    string BillingCycle);

public sealed record SaveProductModuleCompatibilityCommand(
    string? MinimumSafarSuiteVersion,
    string? MinimumLocalServerVersion,
    IReadOnlyCollection<string> SupportedDeploymentModes);

public sealed record SaveProductModuleGroupCommand(
    string GroupId,
    string DisplayName,
    string AccessKind,
    IReadOnlyCollection<string> ModuleCodes);

public sealed record SaveProductResourceCommand(
    string ResourceId,
    string DisplayName,
    string AccessKind,
    IReadOnlyCollection<string> RequiredGroupIds,
    IReadOnlyCollection<string> RequiredModuleCodes);
