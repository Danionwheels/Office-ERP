namespace SafarSuite.ControlDesk.Domain.Modules.Contracts;

public readonly record struct ContractId(Guid Value)
{
    public static ContractId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Contract id cannot be empty.", nameof(value));
        }

        return new ContractId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
