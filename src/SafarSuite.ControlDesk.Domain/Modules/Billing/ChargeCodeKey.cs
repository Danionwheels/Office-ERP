using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Billing;

public sealed class ChargeCodeKey : ValueObject
{
    private ChargeCodeKey(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ChargeCodeKey Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Charge code is required.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length is < 2 or > 32)
        {
            throw new ArgumentException("Charge code must be between 2 and 32 characters.", nameof(value));
        }

        return new ChargeCodeKey(normalized);
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
