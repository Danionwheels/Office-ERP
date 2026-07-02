using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Payments;

public sealed class ClientCreditApplicationReference : ValueObject
{
    private ClientCreditApplicationReference(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ClientCreditApplicationReference Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Credit application reference is required.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > 80)
        {
            throw new ArgumentException("Credit application reference cannot exceed 80 characters.", nameof(value));
        }

        return new ClientCreditApplicationReference(normalized);
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
