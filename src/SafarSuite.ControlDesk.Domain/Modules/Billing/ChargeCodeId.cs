namespace SafarSuite.ControlDesk.Domain.Modules.Billing;

public readonly record struct ChargeCodeId(Guid Value)
{
    public static ChargeCodeId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Charge code id cannot be empty.", nameof(value));
        }

        return new ChargeCodeId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
