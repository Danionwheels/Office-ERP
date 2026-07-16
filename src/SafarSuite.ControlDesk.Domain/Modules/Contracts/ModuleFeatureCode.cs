using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Contracts;

public sealed class ModuleFeatureCode : ValueObject
{
    private ModuleFeatureCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ModuleFeatureCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Feature code is required.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > 64)
        {
            throw new ArgumentException("Feature code cannot exceed 64 characters.", nameof(value));
        }

        if (normalized.Any(character => !char.IsAsciiLetterOrDigit(character)
                                        && character is not '_' and not '-' and not '.'))
        {
            throw new ArgumentException(
                "Feature code can contain only letters, numbers, dots, dashes, and underscores.",
                nameof(value));
        }

        return new ModuleFeatureCode(normalized);
    }

    public override string ToString()
    {
        return Value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
