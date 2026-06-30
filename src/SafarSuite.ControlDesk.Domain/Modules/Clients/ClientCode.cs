using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Clients;

public sealed class ClientCode : ValueObject
{
    private ClientCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ClientCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Client code is required.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length is < 3 or > 32)
        {
            throw new ArgumentException("Client code must be between 3 and 32 characters.", nameof(value));
        }

        return new ClientCode(normalized);
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
