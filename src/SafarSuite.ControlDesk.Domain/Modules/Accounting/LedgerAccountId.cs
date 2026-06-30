namespace SafarSuite.ControlDesk.Domain.Modules.Accounting;

public readonly record struct LedgerAccountId(Guid Value)
{
    public static LedgerAccountId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Ledger account id cannot be empty.", nameof(value));
        }

        return new LedgerAccountId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
