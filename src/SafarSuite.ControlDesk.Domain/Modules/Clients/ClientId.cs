namespace SafarSuite.ControlDesk.Domain.Modules.Clients;

public readonly record struct ClientId(Guid Value)
{
    public static ClientId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Client id cannot be empty.", nameof(value));
        }

        return new ClientId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
