namespace SafarSuite.ControlDesk.Domain.Modules.Payments;

public readonly record struct PaymentId(Guid Value)
{
    public static PaymentId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Payment id cannot be empty.", nameof(value));
        }

        return new PaymentId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
