namespace SafarSuite.ControlDesk.Domain.Modules.Accounting;

public readonly record struct AccountingPeriodId(Guid Value)
{
    public static AccountingPeriodId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Accounting period id cannot be empty.", nameof(value));
        }

        return new AccountingPeriodId(value);
    }
}
