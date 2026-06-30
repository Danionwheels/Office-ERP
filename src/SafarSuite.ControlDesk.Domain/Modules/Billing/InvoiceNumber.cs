using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Billing;

public sealed class InvoiceNumber : ValueObject
{
    private InvoiceNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static InvoiceNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Invoice number is required.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > 40)
        {
            throw new ArgumentException("Invoice number cannot exceed 40 characters.", nameof(value));
        }

        return new InvoiceNumber(normalized);
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
