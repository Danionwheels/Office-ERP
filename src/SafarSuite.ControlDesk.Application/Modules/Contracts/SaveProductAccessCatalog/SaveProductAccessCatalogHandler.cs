using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts.SaveProductAccessCatalog;

public sealed class SaveProductAccessCatalogHandler
{
    private readonly IProductAccessCatalogRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SaveProductAccessCatalogHandler(
        IProductAccessCatalogRepository repository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<SaveProductAccessCatalogResult>> HandleAsync(
        SaveProductAccessCatalogCommand command,
        CancellationToken cancellationToken = default)
    {
        var requestedBy = NormalizeText(command.RequestedBy, 160);
        var errors = ValidateRequestedBy(requestedBy);

        if (errors.Count > 0)
        {
            return Result<SaveProductAccessCatalogResult>.Failure(errors);
        }

        try
        {
            var catalog = NormalizeCatalog(command.ModuleGroups, command.Resources);

            await _repository.SaveAsync(
                catalog,
                requestedBy!,
                _clock.UtcNow,
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<SaveProductAccessCatalogResult>.Success(ToResult(catalog));
        }
        catch (InvalidOperationException exception)
        {
            return Result<SaveProductAccessCatalogResult>.Failure(ApplicationError.Validation(
                nameof(command),
                exception.Message));
        }
    }

    private static IReadOnlyCollection<ApplicationError> ValidateRequestedBy(string? requestedBy)
    {
        if (!string.IsNullOrWhiteSpace(requestedBy))
        {
            return [];
        }

        return
        [
            ApplicationError.Validation(
                nameof(SaveProductAccessCatalogCommand.RequestedBy),
                "Requested by is required before saving the product access catalog.")
        ];
    }

    private static ProductAccessCatalog NormalizeCatalog(
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
        var resourceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var resource in resources)
        {
            if (!resourceIds.Add(resource.ResourceId))
            {
                throw new InvalidOperationException(
                    $"Product access catalog contains duplicate resource id {resource.ResourceId}.");
            }
        }

        return new ProductAccessCatalog(groups, resources);
    }

    private static ProductModuleGroupCatalogItem NormalizeGroup(
        SaveProductModuleGroupCommand command)
    {
        var groupId = NormalizeRequiredText(
            command.GroupId,
            "Product access catalog group id is required.");
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
        var resourceId = NormalizeRequiredText(
            command.ResourceId,
            "Product access catalog resource id is required.");
        var displayName = NormalizeRequiredText(
            command.DisplayName,
            $"Product access catalog resource {resourceId} display name is required.");
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
                    $"Product access catalog resource {resourceId} references unknown group {groupId}.");
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
                $"Product access catalog resource {resourceId} must resolve to at least one module code.");
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
            if (normalized.Length == 0)
            {
                continue;
            }

            if (lookup.Add(normalized))
            {
                ids.Add(normalized);
            }
        }

        return ids;
    }

    private static string NormalizeRequiredText(string value, string errorMessage)
    {
        var normalized = value.Trim();
        if (normalized.Length == 0)
        {
            throw new InvalidOperationException(errorMessage);
        }

        return normalized;
    }

    private static string NormalizeAccessKind(string accessKind)
    {
        if (string.IsNullOrWhiteSpace(accessKind))
        {
            return ProductAccessKinds.PaidModule;
        }

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
        var normalized = value.Trim();

        if (normalized.Length == 0)
        {
            return null;
        }

        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static SaveProductAccessCatalogResult ToResult(ProductAccessCatalog catalog)
    {
        return new SaveProductAccessCatalogResult(
            catalog.ModuleGroups.Select(group => new SavedProductModuleGroupResult(
                    group.GroupId,
                    group.DisplayName,
                    group.AccessKind,
                    group.ModuleCodes.ToArray()))
                .ToArray(),
            catalog.Resources.Select(resource => new SavedProductResourceResult(
                    resource.ResourceId,
                    resource.DisplayName,
                    resource.AccessKind,
                    resource.RequiredGroupIds.ToArray(),
                    resource.RequiredModuleCodes.ToArray(),
                    resource.ResolvedModuleCodes.ToArray()))
                .ToArray());
    }
}

public sealed record SaveProductAccessCatalogCommand(
    IReadOnlyCollection<SaveProductModuleGroupCommand> ModuleGroups,
    IReadOnlyCollection<SaveProductResourceCommand> Resources,
    string RequestedBy);

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

public sealed record SaveProductAccessCatalogResult(
    IReadOnlyCollection<SavedProductModuleGroupResult> ModuleGroups,
    IReadOnlyCollection<SavedProductResourceResult> Resources);

public sealed record SavedProductModuleGroupResult(
    string GroupId,
    string DisplayName,
    string AccessKind,
    IReadOnlyCollection<string> ModuleCodes);

public sealed record SavedProductResourceResult(
    string ResourceId,
    string DisplayName,
    string AccessKind,
    IReadOnlyCollection<string> RequiredGroupIds,
    IReadOnlyCollection<string> RequiredModuleCodes,
    IReadOnlyCollection<string> ResolvedModuleCodes);
