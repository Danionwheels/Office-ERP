namespace SafarSuite.ControlDesk.Domain.Modules.Contracts;

public sealed class ProductCatalogDefinition
{
    private ProductCatalogDefinition(
        IReadOnlyCollection<ProductModuleCatalogItem> modules,
        ProductAccessCatalog accessCatalog)
    {
        Modules = modules;
        AccessCatalog = accessCatalog;
    }

    public IReadOnlyCollection<ProductModuleCatalogItem> Modules { get; }

    public ProductAccessCatalog AccessCatalog { get; }

    public static ProductCatalogDefinition Create(
        IEnumerable<ProductModuleCatalogItem> modules,
        ProductAccessCatalog accessCatalog)
    {
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(accessCatalog);

        var normalizedModules = modules
            .OrderBy(module => module.CommercialMode)
            .ThenBy(module => module.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(module => module.ModuleCode.Value, StringComparer.Ordinal)
            .ToArray();

        if (normalizedModules.Length == 0)
        {
            throw new InvalidOperationException("A product catalog revision requires at least one module definition.");
        }

        var duplicateModule = normalizedModules
            .GroupBy(module => module.ModuleCode.Value, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)?.Key;

        if (duplicateModule is not null)
        {
            throw new InvalidOperationException(
                $"Product catalog contains duplicate module code {duplicateModule}.");
        }

        ValidateAccessCatalog(accessCatalog);

        return new ProductCatalogDefinition(
            normalizedModules,
            new ProductAccessCatalog(
                accessCatalog.ModuleGroups.ToArray(),
                accessCatalog.Resources.ToArray()));
    }

    private static void ValidateAccessCatalog(ProductAccessCatalog accessCatalog)
    {
        var duplicateGroup = accessCatalog.ModuleGroups
            .GroupBy(group => group.GroupId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)?.Key;

        if (duplicateGroup is not null)
        {
            throw new InvalidOperationException(
                $"Product access catalog contains duplicate group id {duplicateGroup}.");
        }

        var duplicateResource = accessCatalog.Resources
            .GroupBy(resource => resource.ResourceId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)?.Key;

        if (duplicateResource is not null)
        {
            throw new InvalidOperationException(
                $"Product access catalog contains duplicate resource id {duplicateResource}.");
        }
    }
}

public sealed class ProductCatalogRevision
{
    private ProductCatalogRevision(
        ProductCatalogRevisionId id,
        long revisionNumber,
        ProductCatalogRevisionId? supersedesRevisionId,
        ProductCatalogDefinition definition,
        string changeReason,
        string publishedBy,
        DateTimeOffset publishedAtUtc)
    {
        Id = id;
        RevisionNumber = revisionNumber;
        SupersedesRevisionId = supersedesRevisionId;
        Definition = definition;
        ChangeReason = changeReason;
        PublishedBy = publishedBy;
        PublishedAtUtc = publishedAtUtc;
    }

    public ProductCatalogRevisionId Id { get; }

    public long RevisionNumber { get; }

    public ProductCatalogRevisionId? SupersedesRevisionId { get; }

    public ProductCatalogDefinition Definition { get; }

    public string ChangeReason { get; }

    public string PublishedBy { get; }

    public DateTimeOffset PublishedAtUtc { get; }

    public static ProductCatalogRevision Publish(
        ProductCatalogRevisionId id,
        long revisionNumber,
        ProductCatalogRevisionId? supersedesRevisionId,
        ProductCatalogDefinition definition,
        string changeReason,
        string publishedBy,
        DateTimeOffset publishedAtUtc)
    {
        if (revisionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(revisionNumber),
                "Product catalog revision number must be greater than zero.");
        }

        if (supersedesRevisionId == id)
        {
            throw new ArgumentException(
                "A product catalog revision cannot supersede itself.",
                nameof(supersedesRevisionId));
        }

        if (revisionNumber == 1 && supersedesRevisionId is not null)
        {
            throw new ArgumentException(
                "The first product catalog revision cannot supersede another revision.",
                nameof(supersedesRevisionId));
        }

        if (revisionNumber > 1 && supersedesRevisionId is null)
        {
            throw new ArgumentException(
                "A later product catalog revision must supersede the previous revision.",
                nameof(supersedesRevisionId));
        }

        ArgumentNullException.ThrowIfNull(definition);

        return new ProductCatalogRevision(
            id,
            revisionNumber,
            supersedesRevisionId,
            definition,
            NormalizeRequired(changeReason, nameof(changeReason), 1000),
            NormalizeRequired(publishedBy, nameof(publishedBy), 160),
            publishedAtUtc);
    }

    private static string NormalizeRequired(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A value is required.", parameterName);
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName);
        }

        return normalized;
    }
}

public sealed class ProductCatalogDraft
{
    private ProductCatalogDraft(
        Guid draftId,
        ProductCatalogRevisionId baseRevisionId,
        long baseRevisionNumber,
        ProductCatalogDefinition definition,
        string changeReason,
        string updatedBy,
        DateTimeOffset updatedAtUtc)
    {
        DraftId = draftId;
        BaseRevisionId = baseRevisionId;
        BaseRevisionNumber = baseRevisionNumber;
        Definition = definition;
        ChangeReason = changeReason;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid DraftId { get; }

    public ProductCatalogRevisionId BaseRevisionId { get; }

    public long BaseRevisionNumber { get; }

    public ProductCatalogDefinition Definition { get; }

    public string ChangeReason { get; }

    public string UpdatedBy { get; }

    public DateTimeOffset UpdatedAtUtc { get; }

    public static ProductCatalogDraft Save(
        Guid draftId,
        ProductCatalogRevisionId baseRevisionId,
        long baseRevisionNumber,
        ProductCatalogDefinition definition,
        string changeReason,
        string updatedBy,
        DateTimeOffset updatedAtUtc)
    {
        if (draftId == Guid.Empty)
        {
            throw new ArgumentException("Product catalog draft id cannot be empty.", nameof(draftId));
        }

        if (baseRevisionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(baseRevisionNumber),
                "Product catalog draft base revision number must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(definition);

        return new ProductCatalogDraft(
            draftId,
            baseRevisionId,
            baseRevisionNumber,
            definition,
            NormalizeRequired(changeReason, nameof(changeReason), 1000),
            NormalizeRequired(updatedBy, nameof(updatedBy), 160),
            updatedAtUtc);
    }

    private static string NormalizeRequired(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A value is required.", parameterName);
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName);
        }

        return normalized;
    }
}
