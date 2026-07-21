using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Contracts;

public sealed class ModuleFeatureLimit : ValueObject
{
    private ModuleFeatureLimit()
    {
        ModuleCode = null!;
        FeatureCode = null!;
        Unit = null!;
    }

    private ModuleFeatureLimit(
        ModuleCode moduleCode,
        ModuleFeatureCode featureCode,
        long limitValue,
        string unit)
    {
        ModuleCode = moduleCode;
        FeatureCode = featureCode;
        LimitValue = limitValue;
        Unit = unit;
    }

    public ModuleCode ModuleCode { get; private set; }

    public ModuleFeatureCode FeatureCode { get; private set; }

    public long LimitValue { get; private set; }

    public string Unit { get; private set; }

    public static ModuleFeatureLimit Create(
        ModuleCode moduleCode,
        ModuleFeatureCode featureCode,
        long limitValue,
        string unit)
    {
        ArgumentNullException.ThrowIfNull(moduleCode);
        ArgumentNullException.ThrowIfNull(featureCode);

        if (limitValue < 0)
        {
            throw new ArgumentException("Feature limit value cannot be negative.", nameof(limitValue));
        }

        var normalizedUnit = NormalizeUnit(unit);

        return new ModuleFeatureLimit(moduleCode, featureCode, limitValue, normalizedUnit);
    }

    public static ModuleFeatureLimit Create(
        string moduleCode,
        string featureCode,
        long limitValue,
        string unit)
    {
        return Create(
            ModuleCode.Create(moduleCode),
            ModuleFeatureCode.Create(featureCode),
            limitValue,
            unit);
    }

    private static string NormalizeUnit(string unit)
    {
        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException("Feature limit unit is required.", nameof(unit));
        }

        var normalized = unit.Trim().ToUpperInvariant();

        if (normalized.Length > 32)
        {
            throw new ArgumentException("Feature limit unit cannot exceed 32 characters.", nameof(unit));
        }

        if (normalized.Any(character => !char.IsAsciiLetterOrDigit(character)
                                        && character is not '_' and not '-'))
        {
            throw new ArgumentException(
                "Feature limit unit can contain only letters, numbers, dashes, and underscores.",
                nameof(unit));
        }

        return normalized;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ModuleCode;
        yield return FeatureCode;
        yield return LimitValue;
        yield return Unit;
    }
}
