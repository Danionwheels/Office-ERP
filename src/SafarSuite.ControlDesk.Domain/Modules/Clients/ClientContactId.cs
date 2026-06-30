namespace SafarSuite.ControlDesk.Domain.Modules.Clients;

public readonly record struct ClientContactId(Guid Value)
{
    public static ClientContactId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Client contact id cannot be empty.", nameof(value));
        }

        return new ClientContactId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
