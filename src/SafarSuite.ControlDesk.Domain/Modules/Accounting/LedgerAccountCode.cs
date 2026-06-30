using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Accounting;

public sealed class LedgerAccountCode : ValueObject
{
    private LedgerAccountCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static LedgerAccountCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Ledger account code is required.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length is < 2 or > 32)
        {
            throw new ArgumentException("Ledger account code must be between 2 and 32 characters.", nameof(value));
        }

        return new LedgerAccountCode(normalized);
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
