namespace SafarSuite.ControlDesk.Domain.Modules.Payments;

public readonly record struct ClientRefundId(Guid Value)
{
    public static ClientRefundId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Client refund id cannot be empty.", nameof(value));
        }

        return new ClientRefundId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
