namespace SafarSuite.ControlDesk.Domain.Modules.Accounting;

public readonly record struct VoucherNumberingRuleId(Guid Value)
{
    public static VoucherNumberingRuleId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Voucher numbering rule id cannot be empty.", nameof(value));
        }

        return new VoucherNumberingRuleId(value);
    }
}
