namespace SafarSuite.ControlDesk.Domain.Modules.Payments;

public readonly record struct PortalPaymentClaimId(Guid Value)
{
    public static PortalPaymentClaimId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Portal payment claim id cannot be empty.", nameof(value));
        }

        return new PortalPaymentClaimId(value);
    }

    public override string ToString() => Value.ToString();
}
