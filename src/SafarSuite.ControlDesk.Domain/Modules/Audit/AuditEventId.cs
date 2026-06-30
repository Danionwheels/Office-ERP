namespace SafarSuite.ControlDesk.Domain.Modules.Audit;

public readonly record struct AuditEventId(Guid Value)
{
    public static AuditEventId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Audit event id cannot be empty.", nameof(value));
        }

        return new AuditEventId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
