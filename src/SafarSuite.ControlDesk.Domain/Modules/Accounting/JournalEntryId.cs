namespace SafarSuite.ControlDesk.Domain.Modules.Accounting;

public readonly record struct JournalEntryId(Guid Value)
{
    public static JournalEntryId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Journal entry id cannot be empty.", nameof(value));
        }

        return new JournalEntryId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
