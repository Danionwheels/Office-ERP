using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Payments;

public sealed class ClientRefundReference : ValueObject
{
    private ClientRefundReference(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ClientRefundReference Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Refund reference is required.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > 80)
        {
            throw new ArgumentException("Refund reference cannot exceed 80 characters.", nameof(value));
        }

        return new ClientRefundReference(normalized);
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
