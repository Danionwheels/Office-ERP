using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Payments;

public sealed class PaymentReference : ValueObject
{
    private PaymentReference(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static PaymentReference Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Payment reference is required.", nameof(value));
        }

        var normalized = value.Trim();

        if (normalized.Length > 80)
        {
            throw new ArgumentException("Payment reference cannot exceed 80 characters.", nameof(value));
        }

        return new PaymentReference(normalized);
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
