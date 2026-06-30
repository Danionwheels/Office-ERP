namespace SafarSuite.ControlDesk.Domain.Modules.Clients;

public readonly record struct ClientAccountingProfileId(Guid Value)
{
    public static ClientAccountingProfileId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Client accounting profile id cannot be empty.", nameof(value));
        }

        return new ClientAccountingProfileId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
