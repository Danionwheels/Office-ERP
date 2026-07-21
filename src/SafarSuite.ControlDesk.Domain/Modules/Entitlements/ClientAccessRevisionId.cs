namespace SafarSuite.ControlDesk.Domain.Modules.Entitlements;

public readonly record struct ClientAccessRevisionId(Guid Value)
{
    public static ClientAccessRevisionId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Client access revision id cannot be empty.", nameof(value));
        }

        return new ClientAccessRevisionId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
