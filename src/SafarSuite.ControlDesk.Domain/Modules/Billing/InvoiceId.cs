namespace SafarSuite.ControlDesk.Domain.Modules.Billing;

public readonly record struct InvoiceId(Guid Value)
{
    public static InvoiceId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Invoice id cannot be empty.", nameof(value));
        }

        return new InvoiceId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
