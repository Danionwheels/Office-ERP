using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Billing;

public sealed class CreditNoteNumber : ValueObject
{
    private CreditNoteNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static CreditNoteNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Credit note number is required.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > 40)
        {
            throw new ArgumentException("Credit note number cannot exceed 40 characters.", nameof(value));
        }

        return new CreditNoteNumber(normalized);
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
