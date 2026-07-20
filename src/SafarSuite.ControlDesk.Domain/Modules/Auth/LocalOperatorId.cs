namespace SafarSuite.ControlDesk.Domain.Modules.Auth;

public readonly record struct LocalOperatorId(Guid Value)
{
    public static LocalOperatorId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Local operator id cannot be empty.", nameof(value));
        }

        return new LocalOperatorId(value);
    }

    public override string ToString() => Value.ToString();
}
