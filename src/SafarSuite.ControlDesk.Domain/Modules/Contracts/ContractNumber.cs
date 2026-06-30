using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Contracts;

public sealed class ContractNumber : ValueObject
{
    private ContractNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ContractNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Contract number is required.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > 40)
        {
            throw new ArgumentException("Contract number cannot exceed 40 characters.", nameof(value));
        }

        return new ContractNumber(normalized);
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
