using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Auth;

public sealed class LocalOperatorEmail : ValueObject
{
    private LocalOperatorEmail(string value, string normalizedValue)
    {
        Value = value;
        NormalizedValue = normalizedValue;
    }

    public string Value { get; }

    public string NormalizedValue { get; }

    public static LocalOperatorEmail Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Local operator email is required.", nameof(value));
        }

        var cleaned = value.Trim();

        if (cleaned.Length > 320)
        {
            throw new ArgumentException("Local operator email cannot exceed 320 characters.", nameof(value));
        }

        var separator = cleaned.IndexOf('@');

        if (separator <= 0
            || separator != cleaned.LastIndexOf('@')
            || separator == cleaned.Length - 1
            || cleaned.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("Local operator email is invalid.", nameof(value));
        }

        return new LocalOperatorEmail(cleaned, cleaned.ToUpperInvariant());
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return NormalizedValue;
    }
}
