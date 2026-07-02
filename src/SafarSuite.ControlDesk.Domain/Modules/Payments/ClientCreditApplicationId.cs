namespace SafarSuite.ControlDesk.Domain.Modules.Payments;

public readonly record struct ClientCreditApplicationId(Guid Value)
{
    public static ClientCreditApplicationId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Client credit application id cannot be empty.", nameof(value));
        }

        return new ClientCreditApplicationId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
