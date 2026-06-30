namespace SafarSuite.ControlDesk.Domain.Modules.ControlCloud;

public readonly record struct CloudOutboxMessageId(Guid Value)
{
    public static CloudOutboxMessageId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Cloud outbox message id cannot be empty.", nameof(value));
        }

        return new CloudOutboxMessageId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
