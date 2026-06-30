namespace SafarSuite.ControlDesk.Domain.Modules.Entitlements;

public readonly record struct EntitlementSnapshotId(Guid Value)
{
    public static EntitlementSnapshotId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Entitlement snapshot id cannot be empty.", nameof(value));
        }

        return new EntitlementSnapshotId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
