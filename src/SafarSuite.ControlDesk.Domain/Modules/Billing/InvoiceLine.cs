using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Billing;

public sealed class InvoiceLine : ValueObject
{
    private InvoiceLine(string description, Money amount, ChargeCodeId? chargeCodeId)
    {
        Description = description;
        Amount = amount;
        ChargeCodeId = chargeCodeId;
    }

    public string Description { get; }

    public Money Amount { get; }

    public ChargeCodeId? ChargeCodeId { get; }

    public static InvoiceLine Create(string description, Money amount)
    {
        return Create(description, amount, null);
    }

    public static InvoiceLine Create(string description, Money amount, ChargeCodeId? chargeCodeId)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Invoice line description is required.", nameof(description));
        }

        if (amount.Amount < 0)
        {
            throw new ArgumentException("Invoice line amount cannot be negative.", nameof(amount));
        }

        return new InvoiceLine(description.Trim(), amount, chargeCodeId);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Description;
        yield return Amount;
        yield return ChargeCodeId;
    }
}
