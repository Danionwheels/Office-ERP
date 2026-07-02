using SafarSuite.ControlDesk.Domain.Modules.Contracts;
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
        ModuleCode? productModuleCode,
        InvoiceLineType lineType)
    {
        Description = description;
        Amount = amount;
        ChargeCodeId = chargeCodeId;
        ProductModuleCode = productModuleCode;
        LineType = lineType;
    }

    public string Description { get; private set; }

    public Money Amount { get; private set; }

    public ChargeCodeId? ChargeCodeId { get; private set; }

    public ModuleCode? ProductModuleCode { get; private set; }

    public InvoiceLineType LineType { get; private set; }

    public static InvoiceLine Create(string description, Money amount)
    {
        return Create(description, amount, null);
    }

    public static InvoiceLine Create(string description, Money amount, ChargeCodeId? chargeCodeId)
    {
        return Create(description, amount, chargeCodeId, null);
    }

    public static InvoiceLine Create(
        string description,
        Money amount,
        ChargeCodeId? chargeCodeId,
        ModuleCode? productModuleCode)
    {
        return Create(description, amount, chargeCodeId, productModuleCode, InvoiceLineType.Charge);
    }

    public static InvoiceLine CreateTax(string description, Money amount, ChargeCodeId chargeCodeId)
    {
        return CreateTax(description, amount, chargeCodeId, null);
    }

    public static InvoiceLine CreateTax(
        string description,
        Money amount,
        ChargeCodeId chargeCodeId,
        ModuleCode? productModuleCode)
    {
        return Create(description, amount, chargeCodeId, productModuleCode, InvoiceLineType.Tax);
    }

    private static InvoiceLine Create(
        string description,
        Money amount,
        ChargeCodeId? chargeCodeId,
        ModuleCode? productModuleCode,
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

        return new InvoiceLine(description.Trim(), amount, chargeCodeId, productModuleCode, lineType);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Description;
        yield return Amount;
        yield return ChargeCodeId;
        yield return ProductModuleCode;
        yield return LineType;
    }
}
