using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Billing;

public sealed class InvoiceLine : ValueObject
{
    private InvoiceLine()
    {
        Description = string.Empty;
        Amount = null!;
        LineType = InvoiceLineType.Charge;
    }

    private InvoiceLine(
        string description,
        Money amount,
        ChargeCodeId? chargeCodeId,
        InvoiceLineType lineType)
    {
        Description = description;
        Amount = amount;
        ChargeCodeId = chargeCodeId;
        LineType = lineType;
    }

    public string Description { get; private set; }

    public Money Amount { get; private set; }

    public ChargeCodeId? ChargeCodeId { get; private set; }

    public InvoiceLineType LineType { get; private set; }

    public static InvoiceLine Create(string description, Money amount)
    {
        return Create(description, amount, null);
    }

    public static InvoiceLine Create(string description, Money amount, ChargeCodeId? chargeCodeId)
    {
        return Create(description, amount, chargeCodeId, InvoiceLineType.Charge);
    }

    public static InvoiceLine CreateTax(string description, Money amount, ChargeCodeId chargeCodeId)
    {
        return Create(description, amount, chargeCodeId, InvoiceLineType.Tax);
    }

    private static InvoiceLine Create(
        string description,
        Money amount,
        ChargeCodeId? chargeCodeId,
        InvoiceLineType lineType)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Invoice line description is required.", nameof(description));
        }

        if (amount.Amount < 0)
        {
            throw new ArgumentException("Invoice line amount cannot be negative.", nameof(amount));
        }

        return new InvoiceLine(description.Trim(), amount, chargeCodeId, lineType);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Description;
        yield return Amount;
        yield return ChargeCodeId;
        yield return LineType;
    }
}
