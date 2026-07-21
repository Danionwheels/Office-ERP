namespace SafarSuite.ControlDesk.Domain.Modules.Accounting;

public readonly record struct OpeningBalanceProfileId(Guid Value)
{
    public static OpeningBalanceProfileId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Opening balance profile id cannot be empty.", nameof(value));
        }

        return new OpeningBalanceProfileId(value);
    }
}
