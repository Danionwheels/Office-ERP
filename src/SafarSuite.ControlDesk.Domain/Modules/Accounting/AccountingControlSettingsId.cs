namespace SafarSuite.ControlDesk.Domain.Modules.Accounting;

public readonly record struct AccountingControlSettingsId(Guid Value)
{
    public static AccountingControlSettingsId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Accounting control settings id cannot be empty.", nameof(value));
        }

        return new AccountingControlSettingsId(value);
    }
}
