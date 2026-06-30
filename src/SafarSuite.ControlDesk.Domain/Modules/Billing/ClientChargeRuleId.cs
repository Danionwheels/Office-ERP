namespace SafarSuite.ControlDesk.Domain.Modules.Billing;

public readonly record struct ClientChargeRuleId(Guid Value)
{
    public static ClientChargeRuleId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Client charge rule id cannot be empty.", nameof(value));
        }

        return new ClientChargeRuleId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
