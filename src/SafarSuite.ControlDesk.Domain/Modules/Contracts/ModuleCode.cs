using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Contracts;

public sealed class ModuleCode : ValueObject
{
    private ModuleCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ModuleCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Module code is required.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > 64)
        {
            throw new ArgumentException("Module code cannot exceed 64 characters.", nameof(value));
        }

        return new ModuleCode(normalized);
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
