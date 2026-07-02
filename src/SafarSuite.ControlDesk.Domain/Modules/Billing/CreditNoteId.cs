namespace SafarSuite.ControlDesk.Domain.Modules.Billing;

public readonly record struct CreditNoteId(Guid Value)
{
    public static CreditNoteId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Credit note id cannot be empty.", nameof(value));
        }

        return new CreditNoteId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
