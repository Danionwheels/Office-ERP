namespace SafarSuite.ControlDesk.Domain.Modules.Clients;

public readonly record struct ClientDeploymentId(Guid Value)
{
    public static ClientDeploymentId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Client deployment id cannot be empty.", nameof(value));
        }

        return new ClientDeploymentId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
