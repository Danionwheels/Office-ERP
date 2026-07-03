namespace SafarSuite.ControlDesk.Domain.Modules.Accounting;

public readonly record struct AccountCodeRangeId(Guid Value)
{
    public static AccountCodeRangeId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Account code range id cannot be empty.", nameof(value));
        }

        return new AccountCodeRangeId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
